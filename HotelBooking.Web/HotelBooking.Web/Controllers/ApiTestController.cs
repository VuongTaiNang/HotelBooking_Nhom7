using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Controllers
{
    public class ApiTestController : Controller
    {
        private readonly HotelApiService _hotelApiService;

        public ApiTestController(
            HotelApiService hotelApiService)
        {
            _hotelApiService = hotelApiService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.Result =
                    await _hotelApiService.CheckHealthAsync();

                ViewBag.Success = true;
            }
            catch (Exception ex)
            {
                ViewBag.Result = ex.Message;
                ViewBag.Success = false;
            }

            return View();
        }
    }
}