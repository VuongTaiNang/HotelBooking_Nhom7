using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace HotelBooking.Controllers;

// API riêng cho luồng đặt phòng bằng tên + số điện thoại.
// Vẫn gọi PKG_HOTEL_BOOKING.SP_CREATE_BOOKING; không nhân đôi nghiệp vụ.
[ApiController]
[Route("api/booking-checkout")]
public sealed class BookingCheckoutController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingCheckoutController> _logger;

    public BookingCheckoutController(IConfiguration configuration,
        ILogger<BookingCheckoutController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public sealed class CheckoutRequest
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = "";

        [Required, RegularExpression(@"^0[0-9]{9}$")]
        public string Phone { get; set; } = "";

        [Range(1, int.MaxValue)]
        public int RoomId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 20)]
        public int GuestCount { get; set; }
    }

    [HttpGet("rooms/{roomId:int}")]
    public async Task<IActionResult> GetRoom(int roomId)
    {
        if (roomId <= 0) return BadRequest(new { message = "Room ID không hợp lệ." });
        try
        {
            await using var connection = new OracleConnection(
                _configuration.GetConnectionString("OracleDb"));
            await connection.OpenAsync();
            const string sql = @"
                SELECT R.ROOM_ID, R.ROOM_NUMBER, RT.TYPE_NAME,
                       RT.PRICE_PER_NIGHT, RT.MAX_GUESTS
                FROM ROOMS R
                JOIN ROOM_TYPES RT ON RT.ROOM_TYPE_ID = R.ROOM_TYPE_ID
                WHERE R.ROOM_ID = :P_ROOM_ID";
            await using var command = new OracleCommand(sql, connection) { BindByName = true };
            command.Parameters.Add("P_ROOM_ID", OracleDbType.Int32).Value = roomId;
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { message = "Không tìm thấy phòng." });

            return Ok(new
            {
                roomId = Convert.ToInt32(reader["ROOM_ID"].ToString(), CultureInfo.InvariantCulture),
                roomNumber = reader["ROOM_NUMBER"].ToString(),
                roomType = reader["TYPE_NAME"].ToString(),
                pricePerNight = Convert.ToDecimal(reader["PRICE_PER_NIGHT"].ToString(), CultureInfo.InvariantCulture),
                maxGuests = Convert.ToInt32(reader["MAX_GUESTS"].ToString(), CultureInfo.InvariantCulture)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to fetch room {RoomId}", roomId);
            return StatusCode(500, new { message = "Không thể tải dữ liệu phòng từ Oracle." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CheckoutRequest request)
    {
        var name = (request.FullName ?? "").Trim();
        var phone = (request.Phone ?? "").Trim();
        if (name.Length < 2 || name.Length > 100 ||
            !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[0-9]{9}$"))
            return BadRequest(new { message = "Họ tên hoặc số điện thoại không hợp lệ." });
        if (request.CheckInDate.Date < DateTime.Today ||
            request.CheckOutDate.Date <= request.CheckInDate.Date)
            return BadRequest(new { message = "Khoảng ngày nhận/trả phòng không hợp lệ." });

        try
        {
            await using var connection = new OracleConnection(
                _configuration.GetConnectionString("OracleDb"));
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            // Kiem tra lai tai API: ngay nhan phong hom nay chi cho AVAILABLE.
            // Khoa row ROOMS cho toi khi transaction tao booking ket thuc.
            var readiness = await HotelBooking.Services.RoomReadinessGuard.CheckForBookingAsync(
                connection, transaction, request.RoomId, request.CheckInDate.Date);
            if (!readiness.Allowed)
                return Conflict(new { success = false, message = readiness.Message });

            // Kiểm tra sức chứa trước khi thay đổi CUSTOMERS.
            const string roomSql = @"
                SELECT R.ROOM_NUMBER, RT.TYPE_NAME, RT.PRICE_PER_NIGHT, RT.MAX_GUESTS
                FROM ROOMS R JOIN ROOM_TYPES RT ON RT.ROOM_TYPE_ID = R.ROOM_TYPE_ID
                WHERE R.ROOM_ID = :P_ROOM_ID AND R.STATUS <> 'MAINTENANCE'";
            string? roomNumber = null;
            string? roomType = null;
            decimal roomRate = 0;
            int maxGuests = 0;
            await using (var roomCmd = new OracleCommand(roomSql, connection)
            {
                BindByName = true, Transaction = transaction
            })
            {
                roomCmd.Parameters.Add("P_ROOM_ID", OracleDbType.Int32).Value = request.RoomId;
                await using var roomReader = await roomCmd.ExecuteReaderAsync();
                if (await roomReader.ReadAsync())
                {
                    roomNumber = roomReader["ROOM_NUMBER"].ToString();
                    roomType = roomReader["TYPE_NAME"].ToString();
                    roomRate = Convert.ToDecimal(roomReader["PRICE_PER_NIGHT"].ToString(), CultureInfo.InvariantCulture);
                    maxGuests = Convert.ToInt32(roomReader["MAX_GUESTS"].ToString(), CultureInfo.InvariantCulture);
                }
            }
            if (roomNumber is null)
                return BadRequest(new { message = "Phòng không tồn tại hoặc đang bảo trì." });
            if (request.GuestCount > maxGuests)
                return BadRequest(new { message = $"Phòng chỉ nhận tối đa {maxGuests} khách." });

            int customerId = 0;
            string? existingName = null;
            string? status = null;
            const string customerSql = @"
                SELECT CUSTOMER_ID, FULL_NAME, STATUS
                FROM CUSTOMERS WHERE PHONE = :P_PHONE FOR UPDATE";
            await using (var lookup = new OracleCommand(customerSql, connection)
            {
                BindByName = true, Transaction = transaction
            })
            {
                lookup.Parameters.Add("P_PHONE", OracleDbType.Varchar2).Value = phone;
                await using var reader = await lookup.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    customerId = Convert.ToInt32(reader["CUSTOMER_ID"].ToString(), CultureInfo.InvariantCulture);
                    existingName = reader["FULL_NAME"].ToString()?.Trim();
                    status = reader["STATUS"].ToString();
                }
            }
            if (customerId > 0)
            {
                if (!string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    return Conflict(new { message = "Hồ sơ khách hàng hiện không hoạt động." });
                if (!string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase))
                    return Conflict(new { message = "Số điện thoại này đã có hồ sơ với họ tên khác. Vui lòng kiểm tra lại." });
            }
            else
            {
                // Identity tự sinh ID. Nếu đặt phòng lỗi, transaction rollback cả customer mới.
                const string insertSql = @"
                    INSERT INTO CUSTOMERS (FULL_NAME, PHONE)
                    VALUES (:P_NAME, :P_PHONE)
                    RETURNING CUSTOMER_ID INTO :P_CUSTOMER_ID";
                await using var insert = new OracleCommand(insertSql, connection)
                {
                    BindByName = true, Transaction = transaction
                };
                insert.Parameters.Add("P_NAME", OracleDbType.Varchar2).Value = name;
                insert.Parameters.Add("P_PHONE", OracleDbType.Varchar2).Value = phone;
                var customerOut = new OracleParameter("P_CUSTOMER_ID", OracleDbType.Int32)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                insert.Parameters.Add(customerOut);
                await insert.ExecuteNonQueryAsync();
                customerId = Convert.ToInt32(customerOut.Value.ToString(), CultureInfo.InvariantCulture);
            }

            const string createSql = @"
                BEGIN
                    PKG_HOTEL_BOOKING.SP_CREATE_BOOKING(
                        P_CUSTOMER_ID => :P_CUSTOMER_ID,
                        P_ROOM_ID => :P_ROOM_ID,
                        P_CHECK_IN_DATE => :P_CHECK_IN_DATE,
                        P_CHECK_OUT_DATE => :P_CHECK_OUT_DATE,
                        P_GUEST_COUNT => :P_GUEST_COUNT,
                        P_BOOKING_ID => :P_BOOKING_ID
                    );
                END;";
            await using var create = new OracleCommand(createSql, connection)
            {
                BindByName = true, Transaction = transaction
            };
            create.Parameters.Add("P_CUSTOMER_ID", OracleDbType.Int32).Value = customerId;
            create.Parameters.Add("P_ROOM_ID", OracleDbType.Int32).Value = request.RoomId;
            create.Parameters.Add("P_CHECK_IN_DATE", OracleDbType.Date).Value = request.CheckInDate.Date;
            create.Parameters.Add("P_CHECK_OUT_DATE", OracleDbType.Date).Value = request.CheckOutDate.Date;
            create.Parameters.Add("P_GUEST_COUNT", OracleDbType.Int32).Value = request.GuestCount;
            var bookingOut = new OracleParameter("P_BOOKING_ID", OracleDbType.Int32)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            create.Parameters.Add(bookingOut);
            await create.ExecuteNonQueryAsync();
            int bookingId = Convert.ToInt32(bookingOut.Value.ToString(), CultureInfo.InvariantCulture);
            transaction.Commit();
            return Ok(new
            {
                success = true,
                bookingId,
                roomNumber,
                roomType,
                roomRate,
                message = "Booking created successfully."
            });
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            return Conflict(new { message = "Số điện thoại vừa được đăng ký đồng thời. Vui lòng gửi lại yêu cầu." });
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
                        _logger.LogWarning(ex, "Booking business validation failed");
            var friendly = ex.Number switch
            {
                20101 => "Phòng đã có booking trùng ngày, vui lòng chọn ngày khác.",
                20123 => "Phòng chưa sẵn sàng nhận khách hôm nay. Vui lòng đợi dọn xong hoặc chọn ngày khác.",
                _ => "Thông tin đặt phòng không hợp lệ. Vui lòng kiểm tra lại."
            };
            return BadRequest(new { message = friendly, oracleCode = ex.Number });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create booking with customer phone");
            return StatusCode(500, new { message = "Không thể tạo booking. Vui lòng kiểm tra kết nối và dữ liệu Oracle." });
        }
    }
}
