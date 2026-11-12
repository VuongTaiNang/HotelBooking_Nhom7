using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelBooking.Web.Models;

public sealed class CreateBookingViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Room không hợp lệ.")]
    public int RoomId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng nhập Customer ID hợp lệ.")]
    [Display(Name = "Customer ID")]
    public int CustomerId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Check-in")]
    public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

    [Required, DataType(DataType.Date), Display(Name = "Check-out")]
    public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);

    [Range(1, 20, ErrorMessage = "Số khách phải từ 1 đến 20.")]
    [Display(Name = "Số khách")]
    public int GuestCount { get; set; } = 1;
}

public sealed class CreateBookingApiRequest
{
    public int CustomerId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
}

public sealed class ApiResult
{
    public bool Success { get; set; }
    public int BookingId { get; set; }
    public string? Message { get; set; }
}

public sealed class BookingHistoryResponse
{
    public bool Success { get; set; }
    public int TotalBookings { get; set; }
    public List<BookingHistoryItem> Bookings { get; set; } = [];
    public string? Message { get; set; }
}

public sealed class BookingHistoryItem
{
    public int BookingId { get; set; }
    public string? RoomNumber { get; set; }
    public string? RoomType { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal RoomRate { get; set; }
    public string? BookingStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class BookingHistoryViewModel
{
    [Required, Phone, Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = "";

    [DataType(DataType.Date), Display(Name = "Từ ngày")]
    public DateTime From { get; set; } = DateTime.Today.AddMonths(-1);

    [DataType(DataType.Date), Display(Name = "Đến ngày")]
    public DateTime To { get; set; } = DateTime.Today.AddDays(1);

    public List<BookingHistoryItem> Bookings { get; set; } = [];
    public string? Error { get; set; }
    public bool HasSearched { get; set; }
}

public sealed class BookingDetailViewModel
{
    public BookingHistoryItem Booking { get; set; } = new();
    public decimal? TotalAmount { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public int ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
}

public sealed class AddServiceApiRequest
{
    public int ServiceId { get; set; }
    public int Quantity { get; set; }
}

public sealed class TotalResponse
{
    public bool Success { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Message { get; set; }
}
