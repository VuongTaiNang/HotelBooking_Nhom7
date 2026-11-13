using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers
{
    public class RoomsController : Controller
    {
        private readonly RoomApiService _roomApiService;

        public RoomsController(RoomApiService roomApiService)
        {
            _roomApiService = roomApiService;
        }

        // GET /Rooms/Search  -> hiện form tìm phòng
        [HttpGet]
        public IActionResult Search()
        {
            var model = new SearchRoomsViewModel
            {
                CheckIn = DateTime.Today,
                CheckOut = DateTime.Today.AddDays(1),
                Guests = 1
            };
            return View(model);
        }

        // GET /Rooms/Available?checkIn=...&checkOut=...&guests=...
        // -> kiểm tra dữ liệu, gọi API, hiện danh sách phòng
        [HttpGet]
        public async Task<IActionResult> Available(SearchRoomsViewModel model)
        {
            if (model.CheckIn.Date < DateTime.Today)
                ModelState.AddModelError(nameof(model.CheckIn), "Ngày nhận phòng không được ở trong quá khứ.");

            if (model.CheckOut.Date <= model.CheckIn.Date)
                ModelState.AddModelError(nameof(model.CheckOut), "Ngày trả phòng phải sau ngày nhận phòng.");

            // Dữ liệu sai -> quay lại form và hiện lỗi
            if (!ModelState.IsValid)
                return View("Search", model);

            var (rooms, error) = await _roomApiService.GetAvailableRoomsAsync(
                model.CheckIn, model.CheckOut, model.Guests);

            var result = new AvailableRoomsViewModel
            {
                Search = model,
                Rooms = rooms,
                Error = error
            };
            return View(result);
        }
    }
}