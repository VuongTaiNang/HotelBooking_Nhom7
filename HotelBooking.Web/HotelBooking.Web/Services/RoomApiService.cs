using System.Globalization;
using System.Text.Json;
using HotelBooking.Web.Models;

namespace HotelBooking.Web.Services
{
    public class RoomApiService
    {
        private readonly HttpClient _httpClient;

        // Cho phép đọc JSON không phân biệt hoa/thường (roomId = RoomId)
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RoomApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Trả về (danh sách phòng, thông báo lỗi). Nếu thành công thì lỗi = null.
        public async Task<(List<RoomDto> Rooms, string? Error)> GetAvailableRoomsAsync(
            DateTime checkIn, DateTime checkOut, int guests)
        {
            var inv = CultureInfo.InvariantCulture;
            var url = $"api/rooms/available" +
                      $"?checkIn={checkIn.ToString("yyyy-MM-dd", inv)}" +
                      $"&checkOut={checkOut.ToString("yyyy-MM-dd", inv)}" +
                      $"&guests={guests}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<RoomDto>(), $"API trả về lỗi {(int)response.StatusCode}: {body}");
                }

                // API trả về thành công nhưng không có nội dung
                if (string.IsNullOrWhiteSpace(body))
                {
                    return (new List<RoomDto>(),
                        $"API trả về dữ liệu rỗng (mã {(int)response.StatusCode}). Vui lòng báo bạn phụ trách Backend kiểm tra.");
                }

                var result = JsonSerializer.Deserialize<RoomSearchResponse>(body, JsonOptions);
                return (result?.Rooms ?? new List<RoomDto>(), null);
            }
            catch (JsonException)
            {
                return (new List<RoomDto>(),
                    "Dữ liệu API trả về không đúng định dạng JSON mong đợi. Vui lòng báo bạn phụ trách Backend.");
            }
            catch (HttpRequestException)
            {
                return (new List<RoomDto>(),
                    "Không kết nối được tới Backend API. Vui lòng kiểm tra Backend và Dev Tunnel đã được bật chưa.");
            }
        }
    }
}