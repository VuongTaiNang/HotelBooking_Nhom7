# Hotel Booking Management — Nhóm 7

Đồ án cuối kỳ môn **Hệ quản trị cơ sở dữ liệu** (HUFLIT). Hệ thống đặt phòng khách sạn minh họa việc sử dụng **Oracle Database 23ai**, **PL/SQL Package**, **Trigger**, **ASP.NET Core Web API** và **ASP.NET Core MVC**.

## 1. Chức năng chính

- Tìm phòng theo ngày và số lượng khách; kiểm tra đặt phòng trùng lịch.
- Tạo booking bằng họ tên, số điện thoại; quản lý các trạng thái `PENDING`, `CONFIRMED`, `CHECKED_IN`, `CHECKED_OUT`, `CANCELLED`.
- Thêm dịch vụ, tính tổng tiền bằng hàm PL/SQL, tra cứu lịch sử đặt phòng.
- Tự động ghi lịch sử thay đổi booking qua Trigger `TRG_BOOKING_AUDIT`.
- Cập nhật trạng thái phòng khi Check-in/Check-out; xác nhận dọn phòng `CLEANING → AVAILABLE` ở trang Housekeeping nội bộ.

## 2. Công nghệ và cấu trúc thư mục

- Cơ sở dữ liệu: **Oracle Database 23ai Free**, PDB `FREEPDB1`, schema `HOTEL_PROJECT`.
- Quản trị và chạy SQL: **DBeaver**; triển khai CSDL bằng **Docker**.
- Backend: **ASP.NET Core Web API (.NET 9)**, `Oracle.ManagedDataAccess.Core`.
- Frontend: **ASP.NET Core MVC (.NET 9)**.

```text
HotelBooking_Nhom7/
├── HotelBooking/             # Web API
├── HotelBooking.Web/         # Website MVC
├── database/                 # SQL/PLSQL (01–08)
├── HotelBooking_Nhom7.sln
└── README.md
```

## 3. Yêu cầu môi trường

1. .NET SDK 9 và Visual Studio hỗ trợ .NET 9 (hoặc `dotnet` CLI).
2. Docker Desktop và Oracle Database 23ai Free đang chạy, cho phép kết nối tại `localhost:1521/FREEPDB1`.
3. DBeaver (hoặc công cụ tương thích với Oracle) và schema có quyền tạo bảng, Package, Procedure, Trigger.
4. Tài khoản Oracle dành cho dự án, ví dụ `HOTEL_PROJECT`, đã được tạo và có quyền thích hợp.

> **Bảo mật:** Không đưa mật khẩu database hoặc khóa Housekeeping vào mã nguồn/Git. Các thông tin nhạy cảm được lưu trong **ASP.NET Core User Secrets** trên máy chạy dự án. Không cần Dev Tunnel khi chạy cả hai project trên cùng một máy.

## 4. Tạo database từ đầu

Kết nối DBeaver với đúng schema dự án trong `FREEPDB1`, sau đó chạy các script bằng chế độ **Execute SQL Script** theo thứ tự:

| Thứ tự | File | Mục đích |
|---|---|---|
| 1 | `database/01_CREATE_TABLES.sql` | Tạo 8 bảng chính và các ràng buộc |
| 2 | `database/02_SEED_DATA.sql` | Thêm dữ liệu mẫu |
| 3 | `database/03_PACKAGE.sql` | Tạo `PKG_HOTEL_BOOKING` (specification và body) |
| 4 | `database/04_TRIGGERS.sql` | Tạo Trigger chống đặt trùng, Audit và đổi trạng thái phòng |
| 5 | `database/07_HOUSEKEEPING.sql` | Thêm bảng log, Procedure hoàn tất dọn phòng và Trigger chặn Check-in khi chưa sẵn sàng |
| 6 | `database/08_BOOKING_SAME_DAY_READINESS.sql` | Thêm kiểm tra phòng sẵn sàng cho booking nhận phòng trong ngày |

**Không chạy `01`–`08` một lần nữa trên schema đã có dữ liệu.** Một số lệnh tạo bảng và chỉ mục không có tính idempotent.

Hai file còn lại **chỉ dùng trong môi trường kiểm thử riêng**:

- `05_TEST_CASES.sql`: các trường hợp kiểm thử nghiệp vụ, có thể phát sinh/thay đổi dữ liệu.
- `06_OPTIMIZATION.sql`: tạo dữ liệu benchmark, điều chỉnh chỉ mục/thống kê và thực hiện các phép đo Execution Plan. **Không chạy cả file trên database demo/nộp bài** nếu chưa xem kỹ các lệnh thay đổi dữ liệu.

Kiểm tra đối tượng PL/SQL:

```sql
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME IN (
    'PKG_HOTEL_BOOKING',
    'TRG_BOOKING_OVERLAP',
    'TRG_BOOKING_AUDIT',
    'TRG_BOOKING_ROOM_STATUS',
    'SP_COMPLETE_ROOM_CLEANING',
    'TRG_BLOCK_CHECKIN_UNREADY',
    'TRG_BOOKING_SAME_DAY_READY'
)
ORDER BY OBJECT_NAME, OBJECT_TYPE;
```

## 5. Cấu hình Backend và Frontend (Windows PowerShell)

Mở PowerShell tại thư mục gốc của repo. Cấu hình chuỗi kết nối Oracle cho Backend bằng User Secrets (thay `<MAT_KHAU_ORACLE_CUA_BAN>` bằng mật khẩu trên **máy của bạn**, không commit vào repo):

```powershell
dotnet user-secrets set "ConnectionStrings:OracleDb" "User Id=HOTEL_PROJECT;Password=<MAT_KHAU_ORACLE_CUA_BAN>;Data Source=localhost:1521/FREEPDB1" --project .\HotelBooking\HotelBooking\HotelBooking.csproj
```

Cho Frontend gọi API nội bộ (khi cả hai chạy cùng máy):

```powershell
dotnet user-secrets set "ApiSettings:BaseUrl" "https://localhost:7083/" --project .\HotelBooking.Web\HotelBooking.Web\HotelBooking.Web.csproj
```

Trang **Housekeeping** yêu cầu cùng một khóa bí mật ở Backend và Frontend. Tạo khóa ngẫu nhiên và lưu trong User Secrets của **cả hai project**:

```powershell
$keyBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($keyBytes)
$rng.Dispose()
$key = [Convert]::ToBase64String($keyBytes)
dotnet user-secrets set "Housekeeping:ApiKey" "$key" --project .\HotelBooking\HotelBooking\HotelBooking.csproj
dotnet user-secrets set "Housekeeping:ApiKey" "$key" --project .\HotelBooking.Web\HotelBooking.Web\HotelBooking.Web.csproj
Remove-Variable key, keyBytes
```

Chỉ sử dụng Housekeeping trên môi trường phát triển nội bộ; **chưa có đăng nhập/phân quyền nhân viên**. Không công khai trang quản lý qua Dev Tunnel hay Internet.

## 6. Build và chạy ứng dụng

```powershell
dotnet build HotelBooking_Nhom7.sln
```

Chạy **cùng lúc** hai project từ Visual Studio bằng chế độ Multiple startup projects, hoặc dùng hai cửa sổ PowerShell riêng:

```powershell
# Cửa sổ 1 — Backend
dotnet run --project .\HotelBooking\HotelBooking\HotelBooking.csproj --launch-profile https
```

```powershell
# Cửa sổ 2 — Frontend
dotnet run --project .\HotelBooking.Web\HotelBooking.Web\HotelBooking.Web.csproj --launch-profile https
```

- Kiểm tra API: `https://localhost:7083/api/database/health` — mong đợi `connected: true`.
- Website: `https://localhost:7085/`.
- Tìm phòng: `/Rooms/Search`; lịch sử: `/Bookings/History`; quản lý dọn phòng nội bộ: `/Housekeeping/Index`.
- Nếu cổng khác các ví dụ trên, sử dụng địa chỉ được ứng dụng in ở dòng `Now listening on`.

## 7. Gợi ý demo nghiệp vụ

1. Tìm một phòng còn lịch trống, chọn ngày nhận/trả phòng và số lượng khách.
2. Đặt phòng bằng họ tên/số điện thoại, quan sát booking được tạo ở trạng thái `PENDING`.
3. Xác nhận booking, Check-in, thêm dịch vụ và xem tổng tiền.
4. Check-out; phòng chuyển sang `CLEANING`.
5. Tại Housekeeping nội bộ, xác nhận hoàn tất dọn phòng; phòng chuyển sang `AVAILABLE` và có log.
6. Xem lịch sử đặt phòng và đối chiếu `BOOKINGS`, `BOOKING_AUDIT`, `ROOM_HOUSEKEEPING_LOG` trong Oracle.

**Lưu ý:** Demo chuyển trạng thái có thể sử dụng ngày đặt phòng giả lập; các kiểm tra ngày/trạng thái phụ thuộc logic thực tế của Package/Trigger. Không sử dụng dữ liệu thật của khách hàng khi trình diễn.

## 8. Thành phần CSDL trọng tâm

- Package `PKG_HOTEL_BOOKING`: tạo, xác nhận, hủy, Check-in, Check-out, thêm dịch vụ, kiểm tra phòng trống và tính tổng tiền.
- `TRG_BOOKING_OVERLAP`: ngăn booking trùng thời gian.
- `TRG_BOOKING_AUDIT`: ghi nhận INSERT/UPDATE/DELETE trên `BOOKINGS`.
- `TRG_BOOKING_ROOM_STATUS`: cập nhật trạng thái phòng theo nghiệp vụ Check-in/Check-out.
- `SP_COMPLETE_ROOM_CLEANING`: xác nhận hoàn tất dọn phòng và ghi log.
- `TRG_BLOCK_CHECKIN_UNREADY`: ngăn Check-in khi phòng chưa sẵn sàng.
- `TRG_BOOKING_SAME_DAY_READY`: kiểm tra trạng thái phòng khi nhận phòng trong ngày.

Đây là đồ án học tập. Trước khi đưa ra Internet hoặc triển khai thực tế cần bổ sung xác thực, phân quyền, bảo vệ dữ liệu khách hàng và kiểm thử bảo mật.
