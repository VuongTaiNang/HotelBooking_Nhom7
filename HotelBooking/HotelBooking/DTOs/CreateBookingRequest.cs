namespace HotelBooking.DTOs
{
    public class CreateBookingRequest
    {
        public int CustomerId { get; set; }

        public int RoomId { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int GuestCount { get; set; }
    }
}
