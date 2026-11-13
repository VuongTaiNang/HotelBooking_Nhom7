# HotelBooking — Hoàn tất dọn phòng (`CLEANING` → `AVAILABLE`)

**Phạm vi:** bổ sung file, KHÔNG ghi đè `Program.cs`, Booking Controller, `appsettings`, bản nâng cấp form họ tên/số điện thoại hoặc dropdown dịch vụ. Môi trường sử dụng: Oracle 23ai Free, schema `HOTEL_PROJECT`, ASP.NET Core 9 API (port local 7083) và MVC web (port local 7085). Chỉ dành cho demo nội bộ, chưa thay thế phân quyền nhân viên thực tế.

## A. Sao lưu và giải nén

Tại PowerShell 5.1:

```powershell
cd D:\HotelBooking_Nhom7
git status
git branch --show-current
# Phải ở integration-final; không được xóa các sửa đổi đang có.
# Sao lưu thư mục repo/copy ZIP trước khi can thiệp nếu đang có thay đổi quan trọng.
Expand-Archive -Path "$env:USERPROFILE\Downloads\HotelBooking_Housekeeping_Final.zip" -DestinationPath "D:\HotelBooking_Nhom7" -Force
```

Gói chỉ có các file MỚI: `database/07_HOUSEKEEPING.sql`, API `HousekeepingController.cs`, MVC controller, model, view, CSS, script tạo khóa API và file hướng dẫn này.

## B. Oracle — chạy script 07

1. Mở DBeaver, chọn kết nối `HOTEL_PROJECT` thuộc PDB `FREEPDB1`.
2. Mở `database/07_HOUSEKEEPING.sql`.
3. Chạy bằng **Execute SQL Script** (không chạy như một lệnh đơn; dấu `/` là dấu kết thúc procedure/trigger).
4. Phải thấy `ROOM_HOUSEKEEPING_LOG`, `SP_COMPLETE_ROOM_CLEANING` và `TRG_BLOCK_CHECKIN_UNREADY` ở trạng thái VALID.

```sql
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS FROM USER_OBJECTS
WHERE OBJECT_NAME IN ('ROOM_HOUSEKEEPING_LOG', 'SP_COMPLETE_ROOM_CLEANING', 'TRG_BLOCK_CHECKIN_UNREADY')
ORDER BY OBJECT_NAME;

SELECT ROOM_ID, ROOM_NUMBER, STATUS FROM ROOMS WHERE STATUS = 'CLEANING' ORDER BY ROOM_NUMBER;
```

Script tạo một bảng log, một procedure, một trigger. **Chỉ chạy lần đầu một lần** vì `CREATE TABLE ROOM_HOUSEKEEPING_LOG` sẽ báo `ORA-00955` nếu tạo lại. Nếu đã tạo bảng rồi và cần cập nhật procedure/trigger, chỉ chạy từ `CREATE OR REPLACE PROCEDURE ...` trở xuống. Không tự ý DROP bảng và không UPDATE phòng thử nghiệm nếu không cần.

Lưu ý: xác nhận dọn xong là hành động của người dùng sau khi kiểm tra phòng, chứ không dựa trên timer; cột `COMPLETED_AT` ghi lại thời điểm xác nhận (không đo thời gian bắt đầu dọn). `COMPLETED_BY` ghi database user `HOTEL_PROJECT`; chưa phải tài khoản nhân viên thực tế vì đồ án chưa có auth.

## C. Cấp khóa API dùng riêng trong môi trường demo

**Quan trọng:** hai project đều cần cùng một khóa, tạo ngẫu nhiên tại máy chạy demo. Frontend `HotelBooking.Web.csproj` gốc chưa có `UserSecretsId`, nên **phải init** cho frontend. Dùng script tự động từ repo:

```powershell
powershell -ExecutionPolicy Bypass -File .\SETUP_HOUSEKEEPING_KEY.ps1
```

Script tạo cùng một khóa ngẫu nhiên trong User Secrets của **hai** project; tự chạy `dotnet user-secrets init` cho frontend và có thể bổ sung `UserSecretsId` vào `HotelBooking.Web.csproj`. Đây là metadata không chứa khóa, **nên commit file `.csproj` vừa đổi**. Không đưa khóa thật vào GitHub, không đặt trong `appsettings.json`. User Secrets chỉ áp dụng môi trường `Development` của máy chạy demo. Chạy lại script sẽ đổi đồng thời khóa ở cả 2 project.

Nếu cần tự chạy tay thay vì script:

```powershell
dotnet user-secrets init --project .\HotelBooking.Web\HotelBooking.Web
$key = [guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N")
dotnet user-secrets set "Housekeeping:ApiKey" "$key" --project .\HotelBooking\HotelBooking
dotnet user-secrets set "Housekeeping:ApiKey" "$key" --project .\HotelBooking.Web\HotelBooking.Web
```

## D. Build, chạy web và API

```powershell
dotnet build HotelBooking_Nhom7.sln
```

Chạy cả 2 project trong Visual Studio, Docker Oracle đã RUNNING. Web sử dụng `ApiSettings:BaseUrl = https://localhost:7083/` trong `HotelBooking.Web/appsettings.Development.json` khi test trên cùng máy.

**Mở trang quản lý nội bộ:**

`https://localhost:7085/Housekeeping/Index`

Giao diện tải tất cả phòng `CLEANING`. Sau khi nhân viên xác nhận, bấm **Hoàn tất dọn phòng** và chấp nhận hộp thoại. Trang tải lại, phòng biến mất khỏi danh sách; Oracle đã chuyển trạng thái thành `AVAILABLE` và thêm log.

Không thêm trang này vào menu khách hàng. Trong bản demo, API có shared key và bật trong `Development`; ứng dụng MVC chưa có xác thực/phân quyền staff thật, do đó **KHÔNG mở Web MVC công khai** hoặc dùng Dev Tunnel public cho tính năng này.

## E. Kiểm thử kèm SQL cần chụp để báo cáo

### TC-HK-01 — thành công

1. Chọn 1 `ROOM_ID` đang `CLEANING` từ SQL ở mục B, ghi ID (ví dụ 5 chỉ khi dữ liệu hiện tại đúng là CLEANING).
2. Chụp SQL trạng thái trước, và danh sách giao diện `/Housekeeping/Index`.
3. Nhấn **Hoàn tất dọn phòng** đúng phòng đó một lần.
4. Kiểm tra:

```sql
SELECT ROOM_ID, ROOM_NUMBER, STATUS
FROM ROOMS
WHERE ROOM_ID = :P_ROOM_ID;

SELECT LOG_ID, ROOM_ID, OLD_STATUS, NEW_STATUS, COMPLETED_AT, COMPLETED_BY
FROM ROOM_HOUSEKEEPING_LOG
WHERE ROOM_ID = :P_ROOM_ID
ORDER BY LOG_ID DESC;
```

**Expected:** `STATUS = AVAILABLE`; một log `CLEANING` → `AVAILABLE` có thời điểm. Thực hiện trong DBeaver: nhập số Room ID thật thay `:P_ROOM_ID` nếu DBeaver hỏi parameter.

### TC-HK-02 — xác nhận hai lần

Gửi lại yêu cầu trực tiếp API bằng PowerShell với khóa vừa tạo ở cùng cửa sổ (hoặc làm mới danh sách, nếu phòng đã biến mất là đúng):

```powershell
# Nếu $key vẫn còn trong PowerShell từ mục C:
$headers = @{ "X-Housekeeping-Key" = $key }
Invoke-WebRequest -Method Post -Uri "https://localhost:7083/api/housekeeping/rooms/5/complete" -Headers $headers
```

Thay `5` bằng Room ID vừa hoàn thành. **Expected:** HTTP 409 (`ORA-20120` bên Oracle); không ghi thêm log, trạng thái vẫn `AVAILABLE`. PowerShell `Invoke-WebRequest` ném lỗi với HTTP 409, đó là kết quả mong đợi.

### TC-HK-03 — Check-in khi phòng đang dọn (negative)

Chỉ thực hiện trên booking kiểm thử `CONFIRMED` của phòng đang `CLEANING`. Gọi Check-in qua web/API hoặc Package. **Expected:** Oracle từ chối `ORA-20122`, booking vẫn `CONFIRMED`, phòng vẫn `CLEANING`. Sau khi nhân viên xác nhận dọn xong → phòng `AVAILABLE`, gọi Check-in lần nữa → booking `CHECKED_IN`, phòng `OCCUPIED` nhờ trigger cũ. Không sử dụng booking có giá trị thực tế.

### TC-HK-04 — Phòng không tồn tại

Gọi `POST /api/housekeeping/rooms/9999999/complete` kèm header key. **Expected:** HTTP 404.

### TC-HK-05 — Thiếu khóa

Gọi `GET /api/housekeeping/rooms/cleaning` qua trình duyệt trực tiếp, không có header. **Expected:** HTTP 401 (không trả danh sách).

## F. Lưu ảnh, Git, chốt source

- **Hình Chương 3:** SQL `SP_COMPLETE_ROOM_CLEANING`, Trigger `TRG_BLOCK_CHECKIN_UNREADY`; màn hình trang Housekeeping.
- **Hình Chương 4:** SQL trước/sau `CLEANING → AVAILABLE`, `ROOM_HOUSEKEEPING_LOG` và test lỗi `ORA-20122`.

Vì repo đang có các thay đổi riêng của bạn (Navbar v2, appsettings, form Name/Phone, Services Dropdown), không chạy `git add .` một cách mù quáng. Chỉ `git add` các file cần thiết và kiểm tra `git diff --cached --stat`, sau đó commit. Không commit password hoặc `Housekeeping:ApiKey`.

```powershell
git status
git add database/07_HOUSEKEEPING.sql
git add HotelBooking/HotelBooking/Controllers/HousekeepingController.cs
git add HotelBooking.Web/HotelBooking.Web/Controllers/HousekeepingController.cs
git add HotelBooking.Web/HotelBooking.Web/Models/HousekeepingViewModels.cs
git add HotelBooking.Web/HotelBooking.Web/Views/Housekeeping/Index.cshtml
git add HotelBooking.Web/HotelBooking.Web/wwwroot/css/housekeeping.css
git add HUONG_DAN_HOUSEKEEPING.md
git add SETUP_HOUSEKEEPING_KEY.ps1
git add HotelBooking.Web/HotelBooking.Web/HotelBooking.Web.csproj
git diff --cached --stat
git commit -m "Add Oracle housekeeping completion workflow"
git push origin integration-final
```

Sau khi kiểm thử PASS và bộ source SQL/PLSQL đầy đủ đã vào repo, mới merge `integration-final` → `master` để nộp.
