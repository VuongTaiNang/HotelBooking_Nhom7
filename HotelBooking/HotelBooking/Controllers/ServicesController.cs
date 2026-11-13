using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace HotelBooking.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServicesController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(IConfiguration configuration, ILogger<ServicesController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // Read-only service catalogue from Oracle. No hard-coded IDs, names or prices.
    [HttpGet]
    public async Task<IActionResult> GetServices(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("OracleDb");
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new OracleCommand(
                "SELECT * FROM SERVICES ORDER BY SERVICE_ID", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var idColumn = FindColumn(reader, "SERVICE_ID");
            var nameColumn = FindColumn(reader, "SERVICE_NAME", "NAME", "SERVICE_TITLE", "TITLE");
            var priceColumn = FindColumn(reader, "PRICE", "UNIT_PRICE", "SERVICE_PRICE", "PRICE_PER_UNIT");

            if (idColumn < 0 || nameColumn < 0)
                throw new InvalidOperationException(
                    "SERVICES must include SERVICE_ID and SERVICE_NAME (or NAME). Please verify the table definition.");

            var services = new List<object>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = Convert.ToInt32(reader.GetValue(idColumn));
                var name = reader.IsDBNull(nameColumn) ? "" : Convert.ToString(reader.GetValue(nameColumn));
                decimal? price = priceColumn < 0 || reader.IsDBNull(priceColumn)
                    ? null : Convert.ToDecimal(reader.GetValue(priceColumn));

                if (string.IsNullOrWhiteSpace(name)) continue;
                services.Add(new { serviceId = id, serviceName = name, price });
            }

            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load hotel service catalogue from Oracle");
            return StatusCode(500, new { message = "Không thể tải danh sách dịch vụ từ Oracle." });
        }
    }

    private static int FindColumn(System.Data.Common.DbDataReader reader, params string[] candidates)
    {
        for (var i = 0; i < reader.FieldCount; i++)
            if (candidates.Any(name => string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase)))
                return i;
        return -1;
    }
}
