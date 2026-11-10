namespace HotelBooking.Web.Models
{
    // Khớp với "cái vỏ" JSON mà API /api/rooms/available trả về:
    // { "checkIn": ..., "checkOut": ..., "guests": ..., "totalRooms": ..., "rooms": [ ... ] }
    public class RoomSearchResponse
    {
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Guests { get; set; }
        public int TotalRooms { get; set; }
        public List<RoomDto> Rooms { get; set; } = new();
    }
}