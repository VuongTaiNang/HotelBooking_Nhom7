using System.Net.Http.Json;
using System.Text.Json;

namespace HotelBooking.Web.Services;

public sealed class HotelApiService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HotelApiService(HttpClient httpClient) => _httpClient = httpClient;

    // Giữ lại endpoint health-check đang được ApiTestController sử dụng.
    public async Task<string> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/database/health", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        => await SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

    public async Task<T?> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return await SendAsync<T>(request, cancellationToken);
    }

    public async Task<T?> PutAsync<T>(string path, CancellationToken cancellationToken = default)
        => await SendAsync<T>(new HttpRequestMessage(HttpMethod.Put, path), cancellationToken);

    private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        using (var response = await _httpClient.SendAsync(request, cancellationToken))
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = TryGetMessage(content) ?? $"API request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
                throw new HotelApiException(message, (int)response.StatusCode);
            }

            if (string.IsNullOrWhiteSpace(content)) return default;
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
    }

    private static string? TryGetMessage(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            foreach (var key in new[] { "message", "title", "detail" })
                if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString();
        }
        catch { }
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }
}

public sealed class HotelApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
