namespace HotelBooking.Web.Models
{
    public class RoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int MaxGuests { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}