# Hotel Booking - Frontend API Guide

## API Base URL

Frontend đọc địa chỉ Backend từ:

`HotelBooking.Web/appsettings.json`

```json
"ApiSettings": {
  "BaseUrl": "<API_BASE_URL>"
}
```

Backend phải đang chạy thì Frontend mới lấy được dữ liệu từ Oracle.

---

## 1. Search Available Rooms

**Method:** GET

```text
/api/rooms/available
```

Parameters:

- `checkIn`
- `checkOut`
- `guests`

Example:

```text
/api/rooms/available?checkIn=2026-12-20&checkOut=2026-12-22&guests=2
```

---

## 2. Create Booking

**Method:** POST

```text
/api/bookings
```

Body:

```json
{
  "customerId": 1,
  "roomId": 2,
  "checkInDate": "2026-12-20",
  "checkOutDate": "2026-12-22",
  "guestCount": 2
}
```

Booking mới được tạo với trạng thái `PENDING`.

---

## 3. Booking History

**Method:** GET

```text
/api/bookings/history
```

Example:

```text
/api/bookings/history?phone=0901000001&from=2026-01-01&to=2027-01-01
```

---

## 4. Confirm Booking

**Method:** PUT

```text
/api/bookings/{bookingId}/confirm
```

Status:

```text
PENDING → CONFIRMED
```

---

## 5. Cancel Booking

**Method:** PUT

```text
/api/bookings/{bookingId}/cancel
```

Status:

```text
PENDING → CANCELLED
```

---

## 6. Check-in

**Method:** PUT

```text
/api/bookings/{bookingId}/check-in
```

Status:

```text
CONFIRMED → CHECKED_IN
```

Room status:

```text
OCCUPIED
```

---

## 7. Add Service

**Method:** POST

```text
/api/bookings/{bookingId}/services
```

Body:

```json
{
  "serviceId": 1,
  "quantity": 2
}
```

Dịch vụ chỉ được thêm khi booking đang ở trạng thái `CHECKED_IN`.

---

## 8. Calculate Booking Total

**Method:** GET

```text
/api/bookings/{bookingId}/total
```

API trả tổng:

```text
Room Cost + Service Cost
```

---

## 9. Check-out

**Method:** PUT

```text
/api/bookings/{bookingId}/check-out
```

Status:

```text
CHECKED_IN → CHECKED_OUT
```

Room status:

```text
CLEANING
```

---

# Booking Flow

Normal flow:

```text
PENDING
   ↓
CONFIRMED
   ↓
CHECKED_IN
   ↓
CHECKED_OUT
```

Cancel flow:

```text
PENDING
   ↓
CANCELLED
```

---

# Frontend Architecture

Frontend không kết nối trực tiếp Oracle.

```text
HotelBooking.Web
       ↓ HTTP
HotelBooking API
       ↓
Oracle Database 23ai
```

Khi test với dữ liệu thật, Backend API và Dev Tunnel trên máy Backend phải đang chạy.