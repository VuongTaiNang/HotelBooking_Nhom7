namespace HotelBooking.Web.Models;

public sealed class CleaningRoomViewModel
{
    public int RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public string? TypeName { get; set; }
    public string? Status { get; set; }
}

public sealed class CleaningRoomsApiResponse
{
    public bool Success { get; set; }
    public int Total { get; set; }
    public List<CleaningRoomViewModel> Rooms { get; set; } = [];
    public string? Message { get; set; }
}

public sealed class CompleteCleaningApiResponse
{
    public bool Success { get; set; }
    public int RoomId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
}

public sealed class HousekeepingIndexViewModel
{
    public List<CleaningRoomViewModel> Rooms { get; set; } = [];
    public string? Error { get; set; }
}
