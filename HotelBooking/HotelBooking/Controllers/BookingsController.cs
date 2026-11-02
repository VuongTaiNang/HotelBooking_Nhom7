using HotelBooking.DTOs;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Oracle.ManagedDataAccess.Types;

namespace HotelBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public BookingsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking(
            CreateBookingRequest request)
        {
            // Validation cơ bản ở API
            if (request.CustomerId <= 0)
            {
                return BadRequest(new
                {
                    message = "CustomerId is invalid."
                });
            }

            if (request.RoomId <= 0)
            {
                return BadRequest(new
                {
                    message = "RoomId is invalid."
                });
            }

            if (request.CheckOutDate <= request.CheckInDate)
            {
                return BadRequest(new
                {
                    message = "Check-out date must be after check-in date."
                });
            }

            if (request.GuestCount <= 0)
            {
                return BadRequest(new
                {
                    message = "Guest count must be greater than 0."
                });
            }

            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                using var transaction =
                    connection.BeginTransaction();

                try
                {
                    const string sql = @"
                        BEGIN
                            PKG_HOTEL_BOOKING.SP_CREATE_BOOKING(
                                P_CUSTOMER_ID    => :P_CUSTOMER_ID,
                                P_ROOM_ID        => :P_ROOM_ID,
                                P_CHECK_IN_DATE  => :P_CHECK_IN_DATE,
                                P_CHECK_OUT_DATE => :P_CHECK_OUT_DATE,
                                P_GUEST_COUNT    => :P_GUEST_COUNT,
                                P_BOOKING_ID     => :P_BOOKING_ID
                            );
                        END;";

                    await using var command =
                        new OracleCommand(sql, connection);

                    command.BindByName = true;
                    command.Transaction = transaction;

                    command.Parameters.Add(
                        "P_CUSTOMER_ID",
                        OracleDbType.Int32
                    ).Value = request.CustomerId;

                    command.Parameters.Add(
                        "P_ROOM_ID",
                        OracleDbType.Int32
                    ).Value = request.RoomId;

                    command.Parameters.Add(
                        "P_CHECK_IN_DATE",
                        OracleDbType.Date
                    ).Value = request.CheckInDate;

                    command.Parameters.Add(
                        "P_CHECK_OUT_DATE",
                        OracleDbType.Date
                    ).Value = request.CheckOutDate;

                    command.Parameters.Add(
                        "P_GUEST_COUNT",
                        OracleDbType.Int32
                    ).Value = request.GuestCount;

                    var bookingIdParameter =
                        new OracleParameter(
                            "P_BOOKING_ID",
                            OracleDbType.Int32
                        )
                        {
                            Direction = ParameterDirection.Output
                        };

                    command.Parameters.Add(bookingIdParameter);

                    await command.ExecuteNonQueryAsync();

                    transaction.Commit();

                    var bookingId =
                        Convert.ToInt32(
                            bookingIdParameter.Value.ToString()
                        );

                    return Ok(new
                    {
                        success = true,
                        bookingId,
                        message = "Booking created successfully."
                    });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (OracleException ex)
                when (ex.Number >= 20000 &&
                      ex.Number <= 20999)
            {
                return BadRequest(new
                {
                    success = false,
                    oracleCode = ex.Number,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPut("{bookingId}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                const string sql = @"
            BEGIN
                PKG_HOTEL_BOOKING.SP_CONFIRM_BOOKING(
                    P_BOOKING_ID => :P_BOOKING_ID
                );
            END;";

                await using var command =
                    new OracleCommand(sql, connection);

                command.BindByName = true;

                command.Parameters.Add(
                    "P_BOOKING_ID",
                    OracleDbType.Int32
                ).Value = bookingId;

                await command.ExecuteNonQueryAsync();

                return Ok(new
                {
                    success = true,
                    bookingId,
                    message = "Booking confirmed successfully."
                });
            }
            catch (OracleException ex)
                when (ex.Number >= 20000 &&
                      ex.Number <= 20999)
            {
                return BadRequest(new
                {
                    success = false,
                    oracleCode = ex.Number,
                    message = ex.Message
                });
            }
        }



        [HttpPut("{bookingId}/check-in")]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                const string sql = @"
            BEGIN
                PKG_HOTEL_BOOKING.SP_CHECK_IN(
                    P_BOOKING_ID => :P_BOOKING_ID
                );
            END;";

                await using var command =
                    new OracleCommand(sql, connection);

                command.BindByName = true;

                command.Parameters.Add(
                    "P_BOOKING_ID",
                    OracleDbType.Int32
                ).Value = bookingId;

                await command.ExecuteNonQueryAsync();

                return Ok(new
                {
                    success = true,
                    bookingId,
                    message = "Guest checked in successfully."
                });
            }
            catch (OracleException ex)
                when (ex.Number >= 20000 &&
                      ex.Number <= 20999)
            {
                return BadRequest(new
                {
                    success = false,
                    oracleCode = ex.Number,
                    message = ex.Message
                });
            }
        }


        [HttpPut("{bookingId}/check-out")]
        public async Task<IActionResult> CheckOut(int bookingId)
        {
            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                const string sql = @"
            BEGIN
                PKG_HOTEL_BOOKING.SP_CHECK_OUT(
                    P_BOOKING_ID => :P_BOOKING_ID
                );
            END;";

                await using var command =
                    new OracleCommand(sql, connection);

                command.BindByName = true;

                command.Parameters.Add(
                    "P_BOOKING_ID",
                    OracleDbType.Int32
                ).Value = bookingId;

                await command.ExecuteNonQueryAsync();

                return Ok(new
                {
                    success = true,
                    bookingId,
                    message = "Guest checked out successfully."
                });
            }
            catch (OracleException ex)
                when (ex.Number >= 20000 &&
                      ex.Number <= 20999)
            {
                return BadRequest(new
                {
                    success = false,
                    oracleCode = ex.Number,
                    message = ex.Message
                });
            }
        }


        [HttpPost("{bookingId}/services")]
        public async Task<IActionResult> AddService(
    int bookingId,
    AddServiceRequest request)
        {
            if (request.ServiceId <= 0)
            {
                return BadRequest(new
                {
                    message = "ServiceId is invalid."
                });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Quantity must be greater than 0."
                });
            }

            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                using var transaction =
                    connection.BeginTransaction();

                try
                {
                    const string sql = @"
                BEGIN
                    PKG_HOTEL_BOOKING.SP_ADD_SERVICE(
                        P_BOOKING_ID => :P_BOOKING_ID,
                        P_SERVICE_ID => :P_SERVICE_ID,
                        P_QUANTITY   => :P_QUANTITY
                    );
                END;";

                    await using var command =
                        new OracleCommand(sql, connection);

                    command.BindByName = true;
                    command.Transaction = transaction;

                    command.Parameters.Add(
                        "P_BOOKING_ID",
                        OracleDbType.Int32
                    ).Value = bookingId;

                    command.Parameters.Add(
                        "P_SERVICE_ID",
                        OracleDbType.Int32
                    ).Value = request.ServiceId;

                    command.Parameters.Add(
                        "P_QUANTITY",
                        OracleDbType.Int32
                    ).Value = request.Quantity;

                    await command.ExecuteNonQueryAsync();

                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        bookingId,
                        serviceId = request.ServiceId,
                        quantity = request.Quantity,
                        message = "Service added successfully."
                    });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (OracleException ex)
                when (ex.Number >= 20000 &&
                      ex.Number <= 20999)
            {
                return BadRequest(new
                {
                    success = false,
                    oracleCode = ex.Number,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet("{bookingId}/total")]
        public async Task<IActionResult> GetBookingTotal(int bookingId)
        {
            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                const string sql = @"
            SELECT
                PKG_HOTEL_BOOKING.FN_CALCULATE_BOOKING_TOTAL(
                    :P_BOOKING_ID
                )
            FROM DUAL";

                await using var command =
                    new OracleCommand(sql, connection);

                command.BindByName = true;

                command.Parameters.Add(
                    "P_BOOKING_ID",
                    OracleDbType.Int32
                ).Value = bookingId;

                var result =
                    await command.ExecuteScalarAsync();

                decimal totalAmount;

                if (result is OracleDecimal oracleDecimal)
                {
                    totalAmount = oracleDecimal.Value;
                }
                else
                {
                    totalAmount =
                        Convert.ToDecimal(result);
                }

                return Ok(new
                {
                    success = true,
                    bookingId,
                    totalAmount
                });
            }
            catch (OracleException ex)
                when (ex.Number >= 20000 &&
                      ex.Number <= 20999)
            {
                return BadRequest(new
                {
                    success = false,
                    oracleCode = ex.Number,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet("history")]
        public async Task<IActionResult> GetBookingHistory(
    string phone,
    DateTime from,
    DateTime to)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest(new
                {
                    message = "Phone number is required."
                });
            }

            if (to <= from)
            {
                return BadRequest(new
                {
                    message = "To date must be after from date."
                });
            }

            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                const string sql = @"
            SELECT
                B.BOOKING_ID,
                R.ROOM_NUMBER,
                RT.TYPE_NAME,
                B.CHECK_IN_DATE,
                B.CHECK_OUT_DATE,
                B.GUEST_COUNT,
                B.ROOM_RATE,
                B.BOOKING_STATUS,
                B.CREATED_AT
            FROM BOOKINGS B

            JOIN ROOMS R
                ON B.ROOM_ID = R.ROOM_ID

            JOIN ROOM_TYPES RT
                ON R.ROOM_TYPE_ID = RT.ROOM_TYPE_ID

            WHERE B.CUSTOMER_ID = (
                SELECT C.CUSTOMER_ID
                FROM CUSTOMERS C
                WHERE C.PHONE = :P_PHONE
            )

            AND B.CREATED_AT >= :P_FROM_DATE
            AND B.CREATED_AT < :P_TO_DATE

            ORDER BY B.CREATED_AT DESC";

                await using var command =
                    new OracleCommand(sql, connection);

                command.BindByName = true;

                command.Parameters.Add(
                    "P_PHONE",
                    OracleDbType.Varchar2
                ).Value = phone;

                command.Parameters.Add(
                    "P_FROM_DATE",
                    OracleDbType.TimeStamp
                ).Value = from;

                command.Parameters.Add(
                    "P_TO_DATE",
                    OracleDbType.TimeStamp
                ).Value = to;

                var bookings = new List<object>();

                await using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bookings.Add(new
                    {
                        bookingId =
                            Convert.ToInt32(reader["BOOKING_ID"]),

                        roomNumber =
                            reader["ROOM_NUMBER"].ToString(),

                        roomType =
                            reader["TYPE_NAME"].ToString(),

                        checkInDate =
                            Convert.ToDateTime(
                                reader["CHECK_IN_DATE"]
                            ),

                        checkOutDate =
                            Convert.ToDateTime(
                                reader["CHECK_OUT_DATE"]
                            ),

                        guestCount =
                            Convert.ToInt32(
                                reader["GUEST_COUNT"]
                            ),

                        roomRate =
                            Convert.ToDecimal(
                                reader["ROOM_RATE"]
                            ),

                        bookingStatus =
                            reader["BOOKING_STATUS"].ToString(),

                        createdAt =
                            Convert.ToDateTime(
                                reader["CREATED_AT"]
                            )
                    });
                }

                return Ok(new
                {
                    success = true,
                    phone,
                    from,
                    to,
                    totalBookings = bookings.Count,
                    bookings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPut("{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var connectionString =
                _configuration.GetConnectionString("OracleDb");

            await using var connection =
                new OracleConnection(connectionString);

            try
            {
                await connection.OpenAsync();

                using var transaction =
                    connection.BeginTransaction();

                try
                {
                    const string sql = @"
                BEGIN
                    PKG_HOTEL_BOOKING.SP_CANCEL_BOOKING(
                        P_BOOKING_ID => :P_BOOKING_ID
                    );
                END;";

                    await using var command =
                        new OracleCommand(sql, connection);

                    command.BindByName = true;
                    command.Transaction = transaction;

                    command.Parameters.Add(
                        "P_BOOKING_ID",
                        OracleDbType.Int32
                    ).Value = bookingId;

                    await command.ExecuteNonQueryAsync();

                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        bookingId,
                        message = "Booking cancelled successfully."
                    });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (OracleException ex)
                when (ex.Number >= 20000 &&
                      ex.Number <= 20999)
            {
                return BadRequest(new
                {
                    success = false,
                    oracleCode = ex.Number,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }





    }
}