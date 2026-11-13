using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Web.Models
{
    // Dữ liệu của form tìm phòng, kèm quy tắc kiểm tra hợp lệ.
    public class SearchRoomsViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckIn { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOut { get; set; }

        [Range(1, 20, ErrorMessage = "Số khách phải từ 1 đến 20.")]
        [Display(Name = "Số khách")]
        public int Guests { get; set; } = 1;
    }
}