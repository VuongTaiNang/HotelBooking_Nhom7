using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

// MVC proxy avoids browser-to-backend CORS and reuses existing typed HttpClient configuration.
[Route("services")]
public sealed class ServicesController : Controller
{
    private readonly HotelApiService _api;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(HotelApiService api, ILogger<ServicesController> logger)
    {
        _api = api;
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try
        {
            var data = await _api.GetAsync<List<ServiceListItem>>("api/services", cancellationToken);
            return Json(data ?? []);
        }
        catch (Exception ex) when (ex is HttpRequestException or HotelApiException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Service list request failed");
            return StatusCode(502, new { message = "Không thể tải danh sách dịch vụ. Hãy kiểm tra kết nối API." });
        }
    }

    public sealed class ServiceListItem
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = "";
        public decimal? Price { get; set; }
    }
}
