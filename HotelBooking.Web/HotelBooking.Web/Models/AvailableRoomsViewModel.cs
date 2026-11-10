namespace HotelBooking.Web.Models
{
    public class AvailableRoomsViewModel
    {
        public SearchRoomsViewModel Search { get; set; } = new();
        public List<RoomDto> Rooms { get; set; } = new();
        public string? Error { get; set; }
    }
}