using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace HotelBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RoomsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableRooms(
            DateTime checkIn,
            DateTime checkOut,
            int guests)
        {
            // Validation đơn giản
            if (checkOut <= checkIn)
            {
                return BadRequest(new
                {
                    message = "Check-out date must be after check-in date."
                });
            }

            if (guests <= 0)
            {
                return BadRequest(new
                {
                    message = "Number of guests must be greater than 0."
                });
            }

            try
            {
                var connectionString =
                    _configuration.GetConnectionString("OracleDb");

                await using var connection =
                    new OracleConnection(connectionString);

                await connection.OpenAsync();

                const string sql = @"
                    SELECT
                        R.ROOM_ID,
                        R.ROOM_NUMBER,
                        RT.TYPE_NAME,
                        RT.PRICE_PER_NIGHT,
                        RT.MAX_GUESTS,
                        R.STATUS
                    FROM ROOMS R
                    JOIN ROOM_TYPES RT
                        ON R.ROOM_TYPE_ID = RT.ROOM_TYPE_ID
                    WHERE R.STATUS <> 'MAINTENANCE'

                      AND RT.MAX_GUESTS >= :P_GUESTS

                      AND NOT EXISTS (
                          SELECT 1
                          FROM BOOKINGS B
                          WHERE B.ROOM_ID = R.ROOM_ID

                            AND B.BOOKING_STATUS IN (
                                'PENDING',
                                'CONFIRMED',
                                'CHECKED_IN'
                            )

                            AND :P_CHECK_IN < B.CHECK_OUT_DATE
                            AND :P_CHECK_OUT > B.CHECK_IN_DATE
                      )

                    ORDER BY RT.PRICE_PER_NIGHT";

                await using var command =
                    new OracleCommand(sql, connection);

                command.BindByName = true;

                command.Parameters.Add(
                    "P_GUESTS",
                    OracleDbType.Int32
                ).Value = guests;

                command.Parameters.Add(
                    "P_CHECK_IN",
                    OracleDbType.Date
                ).Value = checkIn;

                command.Parameters.Add(
                    "P_CHECK_OUT",
                    OracleDbType.Date
                ).Value = checkOut;

                var rooms = new List<object>();

                await using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    rooms.Add(new
                    {
                        roomId =
                            Convert.ToInt32(reader["ROOM_ID"]),

                        roomNumber =
                            reader["ROOM_NUMBER"].ToString(),

                        typeName =
                            reader["TYPE_NAME"].ToString(),

                        pricePerNight =
                            Convert.ToDecimal(
                                reader["PRICE_PER_NIGHT"]
                            ),

                        maxGuests =
                            Convert.ToInt32(
                                reader["MAX_GUESTS"]
                            ),

                        status =
                            reader["STATUS"].ToString()
                    });
                }

                return Ok(new
                {
                    checkIn,
                    checkOut,
                    guests,
                    totalRooms = rooms.Count,
                    rooms
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }
    }
}