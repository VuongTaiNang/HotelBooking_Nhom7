using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace HotelBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DatabaseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("health")]
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                var connectionString =
                    _configuration.GetConnectionString("OracleDb");

                await using var connection =
                    new OracleConnection(connectionString);

                await connection.OpenAsync();

                const string sql = @"
                    SELECT
                        USER AS CURRENT_USER,
                        SYS_CONTEXT('USERENV', 'CON_NAME') AS CONTAINER_NAME
                    FROM DUAL";

                await using var command =
                    new OracleCommand(sql, connection);

                await using var reader =
                    await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        connected = true,
                        currentUser =
                            reader["CURRENT_USER"].ToString(),

                        containerName =
                            reader["CONTAINER_NAME"].ToString(),

                        message =
                            "Connected to Oracle Database successfully."
                    });
                }

                return StatusCode(500, new
                {
                    connected = false,
                    message = "Oracle returned no data."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    connected = false,
                    message = ex.Message
                });
            }
        }
    }
}