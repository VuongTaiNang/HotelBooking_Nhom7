using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace HotelBooking.Controllers;

// Demo internal endpoint: in production replace shared key with proper staff authentication.
[ApiController]
[Route("api/housekeeping")]
public sealed class HousekeepingController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HousekeepingController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    private IActionResult? Guard()
    {
        // Deliberately keep this demo feature disabled outside Development.
        if (!_environment.IsDevelopment()) return NotFound();

        var key = _configuration["Housekeeping:ApiKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            return StatusCode(503, new { success = false, message = "Housekeeping API key has not been configured." });

        var supplied = Request.Headers["X-Housekeeping-Key"].ToString();
        var expectedBytes = Encoding.UTF8.GetBytes(key);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes))
            return Unauthorized(new { success = false, message = "Missing or invalid housekeeping key." });
        return null;
    }

    [HttpGet("rooms/cleaning")]
    public async Task<IActionResult> CleaningRooms(CancellationToken cancellationToken)
    {
        var blocked = Guard();
        if (blocked is not null) return blocked;

        try
        {
            await using var conn = new OracleConnection(_configuration.GetConnectionString("OracleDb"));
            await conn.OpenAsync(cancellationToken);
            const string sql = @"
                SELECT R.ROOM_ID, R.ROOM_NUMBER, RT.TYPE_NAME, R.STATUS
                FROM ROOMS R
                JOIN ROOM_TYPES RT ON RT.ROOM_TYPE_ID = R.ROOM_TYPE_ID
                WHERE R.STATUS = 'CLEANING'
                ORDER BY R.ROOM_NUMBER";
            await using var cmd = new OracleCommand(sql, conn);
            var rooms = new List<object>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rooms.Add(new
                {
                    roomId = Convert.ToInt32(reader["ROOM_ID"]),
                    roomNumber = reader["ROOM_NUMBER"].ToString(),
                    typeName = reader["TYPE_NAME"].ToString(),
                    status = reader["STATUS"].ToString()
                });
            }
            return Ok(new { success = true, total = rooms.Count, rooms });
        }
        catch (OracleException)
        {
            return StatusCode(500, new { success = false, message = "Unable to read cleaning rooms from Oracle." });
        }
    }

    [HttpPost("rooms/{roomId:int}/complete")]
    public async Task<IActionResult> CompleteCleaning(int roomId, CancellationToken cancellationToken)
    {
        var blocked = Guard();
        if (blocked is not null) return blocked;
        if (roomId <= 0) return BadRequest(new { success = false, message = "Room ID must be positive." });

        await using var conn = new OracleConnection(_configuration.GetConnectionString("OracleDb"));
        try
        {
            await conn.OpenAsync(cancellationToken);
            using var tx = conn.BeginTransaction();
            try
            {
                await using var cmd = new OracleCommand(
                    "BEGIN SP_COMPLETE_ROOM_CLEANING(:P_ROOM_ID); END;", conn)
                {
                    BindByName = true,
                    Transaction = tx
                };
                cmd.Parameters.Add("P_ROOM_ID", OracleDbType.Int32).Value = roomId;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                tx.Commit();
                return Ok(new { success = true, roomId, status = "AVAILABLE", message = "Đã xác nhận dọn phòng hoàn tất." });
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (OracleException ex) when (ex.Number == 20121)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phòng." });
        }
        catch (OracleException ex) when (ex.Number == 20120)
        {
            return Conflict(new { success = false, message = "Phòng không ở trạng thái Đang dọn. Không thể xác nhận lần nữa." });
        }
        catch (OracleException ex) when (ex.Number == 30006 || ex.Number == 54)
        {
            return Conflict(new { success = false, message = "Phòng đang được xử lý ở một phiên khác. Hãy thử lại." });
        }
        catch (OracleException)
        {
            return StatusCode(500, new { success = false, message = "Không thể cập nhật trạng thái phòng. Kiểm tra script Oracle." });
        }
    }
}
