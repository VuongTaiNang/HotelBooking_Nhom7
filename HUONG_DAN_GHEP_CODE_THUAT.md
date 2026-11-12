# Import phần frontend booking của Thuật (đã lọc để không ghi đè phần Khôi)

Nguồn: HotelBookingWeb_Thuat.zip (Thuật). Các file .vs/, bin/, obj/, DLL, .sln và source trùng chức năng Home/Rooms đã được loại.

## Đưa lên branch frontend-booking

1. Tại repo gốc HotelBooking_Nhom7, chạy:
   git fetch origin
   git switch frontend-booking
   git pull origin frontend-booking
2. Giải nén ZIP này *ngay vào thư mục gốc của repo*. Các đường dẫn bên trong ZIP đã căn đúng theo cấu trúc repo hiện tại `HotelBooking.Web/HotelBooking.Web/`.
3. Lệnh: git status; dotnet build HotelBooking_Nhom7.sln
4. Nếu build thành công: 
   git add HotelBooking.Web/HotelBooking.Web/Controllers/BookingsController.cs HotelBooking.Web/HotelBooking.Web/Models/BookingViewModels.cs HotelBooking.Web/HotelBooking.Web/Services/HotelApiService.cs HotelBooking.Web/HotelBooking.Web/Views/Bookings/ HotelBooking.Web/HotelBooking.Web/wwwroot/css/booking.css HotelBooking.Web/HotelBooking.Web/THUAT-FRONTEND-NOTES.md
   git commit -m "Integrate Thuat booking frontend"
   git push -u origin frontend-booking
5. Trên GitHub tạo PR frontend-booking -> master hoặc merge vào integration-final sau khi đã review/kiểm thử.

## Giữ nguyên phần Khôi
Không thay thế: Views/Home/Index.cshtml, Views/Shared/_Layout.cshtml, Program.cs, appsettings.json, RoomsController.cs, RoomApiService.cs.

## Những chỉnh sửa có chủ đích so với ZIP Thuật
- Đổi stylesheet của Thuật thành `wwwroot/css/booking.css`; chỉ load trên 3 trang Booking để tránh đè `site.css`/Home của Khôi.
- Create GET nhận thêm `checkIn`, `checkOut`, `guests` tùy chọn từ trang tìm phòng. Khôi cần cập nhật link Book Now để truyền các query parameter này.
- Chuyển TotalResponse thành public model để System.Text.Json deserialize đáng tin cậy.

## Giới hạn cần kiểm tra
- Detail chưa có GET /api/bookings/{id}; dữ liệu hiển thị tạm lấy từ kết quả tạo/lịch sử. Một số giá trị có thể chưa cập nhật theo DB.
- Create yêu cầu CustomerId có sẵn, Add Service yêu cầu ServiceId có sẵn.
- Bắt buộc thử luồng Home -> Search -> Available -> Create -> History/Detail -> Confirm -> Check-in -> Add Service -> Total -> Check-out, cùng luồng Cancel riêng.
- Không push password Oracle vào GitHub; backend dùng User Secrets.
