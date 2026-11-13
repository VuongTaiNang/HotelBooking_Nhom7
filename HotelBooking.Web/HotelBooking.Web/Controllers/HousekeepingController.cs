using System.Net.Http.Json;
using System.Text.Json;
using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers;

// Demo-only internal page. Do not publicly expose the MVC site until staff auth is implemented.
[Route("[controller]/[action]")]
public sealed class HousekeepingController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;

    public HousekeepingController(
        IConfiguration configuration, IHttpClientFactory httpClientFactory, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
    }

    private HttpClient CreateApiClient()
    {
        var baseUrl = _configuration["ApiSettings:BaseUrl"];
        var key = _configuration["Housekeeping:ApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl chưa được cấu hình.");
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("Chưa cấu hình Housekeeping:ApiKey trong User Secrets.");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("X-Housekeeping-Key", key);
        return client;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        var model = new HousekeepingIndexViewModel();
        try
        {
            using var client = CreateApiClient();
            using var response = await client.GetAsync("api/housekeeping/rooms/cleaning", cancellationToken);
            if (!response.IsSuccessStatusCode)
                model.Error = await ReadMessageAsync(response, cancellationToken);
            else
            {
                var result = await response.Content.ReadFromJsonAsync<CleaningRoomsApiResponse>(cancellationToken);
                model.Rooms = result?.Rooms ?? [];
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException)
        {
            model.Error = ex is InvalidOperationException ? ex.Message : "Không thể lấy danh sách phòng đang dọn từ API.";
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int roomId, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        if (roomId <= 0)
        {
            TempData["Error"] = "Room ID không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var client = CreateApiClient();
            using var response = await client.PostAsync($"api/housekeeping/rooms/{roomId}/complete", content: null, cancellationToken: cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<CompleteCleaningApiResponse>(cancellationToken);
            if (response.IsSuccessStatusCode && result?.Success == true)
                TempData["Success"] = $"Phòng #{roomId}: {result.Message}";
            else
                TempData["Error"] = result?.Message ?? $"API trả lỗi HTTP {(int)response.StatusCode}.";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException)
        {
            TempData["Error"] = ex is InvalidOperationException ? ex.Message : "Không thể kết nối API để cập nhật phòng.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static async Task<string> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (doc.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? "API trả lỗi.";
        }
        catch (JsonException) { }
        return $"API trả lỗi HTTP {(int)response.StatusCode}.";
    }
}
