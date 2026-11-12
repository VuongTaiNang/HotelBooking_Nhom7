using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace HotelBooking.Web.Controllers;

[Route("[controller]/[action]")]
public sealed class BookingsController : Controller
{
    private readonly HotelApiService _api;

    public BookingsController(HotelApiService api) => _api = api;

    [HttpGet]
    public IActionResult Create(int roomId, DateTime? checkIn = null, DateTime? checkOut = null, int? guests = null)
    {
        if (roomId <= 0) return BadRequest("Room ID không hợp lệ.");

        var arrival = checkIn?.Date ?? DateTime.Today.AddDays(1);
        var departure = checkOut?.Date ?? arrival.AddDays(1);
        return View(new CreateBookingViewModel
        {
            RoomId = roomId,
            CheckInDate = arrival,
            CheckOutDate = departure,
            GuestCount = guests is > 0 ? guests.Value : 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model, CancellationToken cancellationToken)
    {
        if (model.CheckOutDate.Date <= model.CheckInDate.Date)
            ModelState.AddModelError(nameof(model.CheckOutDate), "Check-out phải sau check-in.");

        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await _api.PostAsync<ApiResult>("api/bookings", new CreateBookingApiRequest
            {
                CustomerId = model.CustomerId,
                RoomId = model.RoomId,
                CheckInDate = model.CheckInDate.Date,
                CheckOutDate = model.CheckOutDate.Date,
                GuestCount = model.GuestCount
            }, cancellationToken);

            if (result?.Success == true)
            {
                TempData["Success"] = $"Đặt phòng thành công. Mã booking: #{result.BookingId}.";
                return RedirectToAction(nameof(Detail), new
                {
                    bookingId = result.BookingId,
                    roomNumber = "—",
                    roomType = "Đặt phòng mới",
                    checkInDate = model.CheckInDate.ToString("yyyy-MM-dd"),
                    checkOutDate = model.CheckOutDate.ToString("yyyy-MM-dd"),
                    guestCount = model.GuestCount,
                    roomRate = 0,
                    bookingStatus = "PENDING"
                });
            }

            ModelState.AddModelError("", result?.Message ?? "Không thể tạo booking.");
        }
        catch (HotelApiException ex) { ModelState.AddModelError("", ex.Message); }
        catch (HttpRequestException) { ModelState.AddModelError("", "Không thể kết nối HotelBooking API. Kiểm tra backend/Dev Tunnel."); }

        return View(model);
    }

    [HttpGet]
    public IActionResult History()
    {
        return View(new BookingHistoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> History(BookingHistoryViewModel model, CancellationToken cancellationToken)
    {
        model.HasSearched = true;
        model.Bookings = [];
        // Người dùng chọn cả ngày bắt đầu và ngày kết thúc (inclusive).
        // Backend dùng CREATED_AT < :P_TO_DATE nên gửi ngày kế tiếp làm cận trên exclusive.
        if (model.To.Date < model.From.Date)
            ModelState.AddModelError(nameof(model.To), "Đến ngày phải bằng hoặc sau Từ ngày.");

        if (!ModelState.IsValid) return View(model);

        try
        {
            var query = $"api/bookings/history?phone={Uri.EscapeDataString(model.Phone)}" +
                        $"&from={Uri.EscapeDataString(model.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}" +
                        $"&to={Uri.EscapeDataString(model.To.Date.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}";
            var result = await _api.GetAsync<BookingHistoryResponse>(query, cancellationToken);
            if (result?.Success == false)
                model.Error = result.Message ?? "Backend không thể tra cứu lịch sử booking.";
            else
                model.Bookings = result?.Bookings ?? [];
        }
        catch (HotelApiException ex) { model.Error = ex.Message; }
        catch (HttpRequestException) { model.Error = "Không thể kết nối HotelBooking API. Kiểm tra backend/Dev Tunnel."; }

        return View(model);
    }

    // Backend hiện chưa cung cấp GET /api/bookings/{id}; trang detail nhận snapshot từ lịch sử hoặc kết quả tạo booking.
    [HttpGet]
    public IActionResult Detail(int bookingId, string? roomNumber, string? roomType,
        DateTime? checkInDate, DateTime? checkOutDate, int guestCount = 1,
        decimal roomRate = 0, string? bookingStatus = "PENDING")
    {
        if (bookingId <= 0) return BadRequest("Booking ID không hợp lệ.");
        return View(new BookingDetailViewModel
        {
            Booking = new BookingHistoryItem
            {
                BookingId = bookingId, RoomNumber = roomNumber, RoomType = roomType,
                CheckInDate = checkInDate ?? DateTime.MinValue,
                CheckOutDate = checkOutDate ?? DateTime.MinValue,
                GuestCount = guestCount, RoomRate = roomRate, BookingStatus = bookingStatus
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Operation(int bookingId, string operation, string? roomNumber, string? roomType,
        DateTime? checkInDate, DateTime? checkOutDate, int guestCount, decimal roomRate, string? bookingStatus,
        CancellationToken cancellationToken)
    {
        var endpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["confirm"] = $"api/bookings/{bookingId}/confirm",
            ["cancel"] = $"api/bookings/{bookingId}/cancel",
            ["check-in"] = $"api/bookings/{bookingId}/check-in",
            ["check-out"] = $"api/bookings/{bookingId}/check-out"
        };

        var normalizedOperation = operation?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!endpoints.TryGetValue(normalizedOperation, out var endpoint))
            return BadRequest("Thao tác không hợp lệ.");

        try
        {
            var result = await _api.PutAsync<ApiResult>(endpoint, cancellationToken);
            EnsureApiSuccess(result, $"Không thể thực hiện thao tác {normalizedOperation}.");

            var updatedStatus = normalizedOperation switch
            {
                "confirm" => "CONFIRMED",
                "cancel" => "CANCELLED",
                "check-in" => "CHECKED_IN",
                "check-out" => "CHECKED_OUT",
                _ => bookingStatus
            };

            bookingStatus = updatedStatus;
            TempData["Success"] = result?.Message
                ?? $"Đã thực hiện thao tác {normalizedOperation} thành công.";
        }
        catch (HotelApiException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Không thể kết nối HotelBooking API.";
        }

        return RedirectToAction(nameof(Detail), new
        {
            bookingId,
            roomNumber,
            roomType,
            checkInDate,
            checkOutDate,
            guestCount,
            roomRate,
            bookingStatus
        });

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService(int bookingId, int serviceId, int quantity, string? roomNumber, string? roomType,
        DateTime? checkInDate, DateTime? checkOutDate, int guestCount, decimal roomRate, string? bookingStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            if (serviceId <= 0 || quantity <= 0)
                throw new HotelApiException("Service ID và số lượng phải lớn hơn 0.", 400);
            var result = await _api.PostAsync<ApiResult>($"api/bookings/{bookingId}/services",
                new AddServiceApiRequest { ServiceId = serviceId, Quantity = quantity }, cancellationToken);
            EnsureApiSuccess(result, "Backend không thể thêm dịch vụ.");
            TempData["Success"] = result?.Message ?? "Đã thêm dịch vụ.";
        }
        catch (HotelApiException ex) { TempData["Error"] = ex.Message; }
        catch (HttpRequestException) { TempData["Error"] = "Không thể kết nối HotelBooking API."; }
        return RedirectToAction(nameof(Detail), new { bookingId, roomNumber, roomType, checkInDate, checkOutDate, guestCount, roomRate, bookingStatus });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateTotal(int bookingId, string? roomNumber, string? roomType,
        DateTime? checkInDate, DateTime? checkOutDate, int guestCount, decimal roomRate, string? bookingStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _api.GetAsync<TotalResponse>($"api/bookings/{bookingId}/total", cancellationToken);
            if (result?.Success == false)
                throw new HotelApiException(result.Message ?? "Backend không thể tính tổng booking.", 400);
            TempData["TotalAmount"] = result?.TotalAmount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) ?? "Không có dữ liệu";
            TempData["Success"] = "Đã tính tổng booking.";
        }
        catch (HotelApiException ex) { TempData["Error"] = ex.Message; }
        catch (HttpRequestException) { TempData["Error"] = "Không thể kết nối HotelBooking API."; }
        return RedirectToAction(nameof(Detail), new { bookingId, roomNumber, roomType, checkInDate, checkOutDate, guestCount, roomRate, bookingStatus });
    }

    private static void EnsureApiSuccess(ApiResult? result, string fallbackMessage)
    {
        if (result?.Success != true)
            throw new HotelApiException(result?.Message ?? fallbackMessage, 400);
    }

}
