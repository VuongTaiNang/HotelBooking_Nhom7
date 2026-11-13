# HotelBooking - Form nhập họ tên và số điện thoại (bản hoàn thiện trước nộp)

## Các file cần chép

Giải nén ZIP **ngay tại D:\HotelBooking_Nhom7**. Gói có 4 file tương ứng với project hiện tại:

- HotelBooking/HotelBooking/Controllers/BookingCheckoutController.cs (mới)
- HotelBooking.Web/HotelBooking.Web/Controllers/BookingsController.cs (thay thế)
- HotelBooking.Web/HotelBooking.Web/Models/BookingViewModels.cs (thay thế)
- HotelBooking.Web/HotelBooking.Web/Views/Bookings/Create.cshtml (thay thế)

**Không ghi đè** Program.cs, appsettings*, Views/Rooms/Available.cshtml, Views/Shared/_Layout.cshtml hoặc script PL/SQL. Các thay đổi Navbar v2 và UI polish không bị ảnh hưởng.

## Cách triển khai
1. Giữ nguyên branch `integration-final`. Chạy `git status` và sao lưu thay đổi chưa commit.
2. Dừng hai project trong Visual Studio. Giải nén vào `D:\HotelBooking_Nhom7` với `-Force`.
3. Chạy `dotnet build HotelBooking_Nhom7.sln`. Sau khi build thành công, chạy `HotelBooking` và `HotelBooking.Web`.
4. Kiểm tra health `https://localhost:7083/api/database/health`.
5. Kiểm tra GET `https://localhost:7083/api/booking-checkout/rooms/5`, thông tin Room 202 – Deluxe – 1200000 đ/đêm trong bộ dữ liệu test trước.
6. Trên web, chọn phòng và nhập **họ tên + số điện thoại** (không nhập Customer ID). Dùng **số điện thoại test mới** chưa có trong CUSTOMERS để thử tạo khách, ví dụ số thuộc dải kiểm thử do nhóm tự quản lý.
7. Sau khi đặt thành công, kiểm tra `CUSTOMERS`, `BOOKINGS`, `BOOKING_AUDIT`. Nếu khách cũ, nhập tên khớp FULL_NAME trong Oracle và cùng PHONE để kiểm tra không tạo trùng.

## Chú ý dữ liệu và bảo mật
- `CUSTOMERS` thực tế phải có: CUSTOMER_ID identity, FULL_NAME, PHONE unique, STATUS default ACTIVE. Nếu DB của bạn khác script thiết kế, đối chiếu DDL trước khi chạy.
- Endpoint này dành cho demo nội bộ. Vì hệ thống chưa có đăng nhập/xác minh sở hữu số điện thoại, không triển khai public để người lạ mạo danh khách. Giữ Backend trên localhost, hạn chế Dev Tunnel public.
- Tránh thử bằng số điện thoại người thật không thuộc dữ liệu demo. Không cung cấp API liệt kê khách hàng công khai.
- Nếu lỗi ORA-xxxxx, xem lỗi trả về và không tự sửa Package đã test PASS.

## Các trường hợp thử sau khi vá
| ID | Dữ liệu | Mong đợi |
|---|---|---|
| CT-01 | Tên + số ĐT mới, phòng còn trống, ngày hợp lệ | Tạo CUSTOMERS + BOOKINGS trong 1 transaction, PENDING |
| CT-02 | Tên + số ĐT cũ trùng đúng hồ sơ | Tạo BOOKING mới, không tạo thêm CUSTOMERS |
| CT-03 | Số ĐT cũ nhưng tên khác | 409 Conflict, không tạo booking |
| CT-04 | SĐT sai định dạng | Form/API validation, không ghi DB |
| CT-05 | Booking trùng phòng/ngày | Package từ chối, không tạo customer rác |
| CT-06 | Số khách > MAX_GUESTS | Từ chối trước khi tạo customer |

## Chốt Git sau khi test
```
git status
git add HotelBooking/HotelBooking/Controllers/BookingCheckoutController.cs HotelBooking.Web/HotelBooking.Web/Controllers/BookingsController.cs HotelBooking.Web/HotelBooking.Web/Models/BookingViewModels.cs HotelBooking.Web/HotelBooking.Web/Views/Bookings/Create.cshtml
git commit -m "Use customer name and phone for booking checkout"
git push origin integration-final
```
Không chạy `git add .` vì appsettings.json có thay đổi cục bộ. Đừng merge master trước khi kiểm thử.
