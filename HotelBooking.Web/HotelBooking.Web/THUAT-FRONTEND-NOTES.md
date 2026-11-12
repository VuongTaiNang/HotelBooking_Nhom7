# HotelBooking.Web — phần frontend booking của Thuật

## Chức năng
- Tạo booking tại `/Bookings/Create?roomId=<id>`.
- Tra cứu lịch sử tại `/Bookings/History` bằng số điện thoại và khoảng ngày tạo booking.
- Trang chi tiết booking hỗ trợ Confirm, Cancel, Check-in, Add Service, Calculate Total và Check-out.
- Frontend gọi backend server-side qua `HotelApiService`; trình duyệt không kết nối Oracle trực tiếp.

## API contract đã đối chiếu
Đã đọc `FRONTEND_API.md` ở thư mục gốc repository và đối chiếu với DTO/controller backend:
- `POST api/bookings`: body gồm `customerId`, `roomId`, `checkInDate`, `checkOutDate`, `guestCount`.
- `GET api/bookings/history?phone=...&from=...&to=...`: response có `success`, `totalBookings`, `bookings`; mỗi booking có `bookingId`, `roomNumber`, `roomType`, `checkInDate`, `checkOutDate`, `guestCount`, `roomRate`, `bookingStatus`, `createdAt`.
- History backend dùng `CREATED_AT >= from AND CREATED_AT < to`; UI nhận ngày kết thúc inclusive và gửi `to + 1 ngày` làm cận trên exclusive. Cho phép tìm đúng một ngày.
- `PUT api/bookings/{id}/confirm|cancel|check-in|check-out` trả `success`, `bookingId`, `message`.
- `POST api/bookings/{id}/services`: body `serviceId`, `quantity`; response có `success` và `message`.
- `GET api/bookings/{id}/total`: response có `success`, `bookingId`, `totalAmount`.

## Giới hạn backend hiện tại
- `POST api/bookings` yêu cầu `CustomerId`; form yêu cầu nhập ID khách hàng có sẵn vì hiện chưa có API tra cứu/tạo khách hàng.
- Backend chưa có `GET api/bookings/{id}`. Trang Detail nhận snapshot từ kết quả tạo booking hoặc lịch sử; trạng thái được cập nhật cục bộ sau các thao tác thành công. Booking vừa tạo chưa có API trả thông tin phòng/giá nên hiển thị placeholder.
- Chưa có API danh sách dịch vụ nên form Add Service vẫn nhận `Service ID` thủ công.

## Tương thích phần chung
`HotelApiService.CheckHealthAsync()` được giữ lại để `ApiTestController` hiện có tiếp tục hoạt động; các method GET/POST/PUT được bổ sung song song.

## Build
Chưa thể chạy `dotnet build` trong môi trường chỉnh sửa vì không có .NET SDK cài đặt. Cần build lại bằng .NET 9 SDK trong môi trường dự án.
