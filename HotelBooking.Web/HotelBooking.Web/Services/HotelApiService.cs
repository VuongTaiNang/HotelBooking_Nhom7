namespace HotelBooking.Web.Services
{
    public class HotelApiService
    {
        private readonly HttpClient _httpClient;

        public HotelApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CheckHealthAsync()
        {
            var response =
                await _httpClient.GetAsync("api/database/health");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}