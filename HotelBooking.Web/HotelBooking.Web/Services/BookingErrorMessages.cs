namespace HotelBooking.Web.Services;

public static class BookingErrorMessages
{
    // Phần giải thích dành cho người dùng, không hiển thị stack Oracle/ORA-06512.
    public static string Friendly(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Thao tác chưa thành công. Vui lòng thử lại.";

        if (!message.Contains("ORA-", StringComparison.OrdinalIgnoreCase))
            return message;

        if (message.Contains("ORA-20019", StringComparison.OrdinalIgnoreCase))
            return "Chỉ có thể thêm dịch vụ sau khi khách đã Check-in.";
        if (message.Contains("ORA-20010", StringComparison.OrdinalIgnoreCase))
            return "Booking chỉ được xác nhận một lần khi ở trạng thái Chờ xác nhận.";
        if (message.Contains("ORA-20014", StringComparison.OrdinalIgnoreCase))
            return "Cần xác nhận booking trước khi Check-in.";
        if (message.Contains("ORA-20125", StringComparison.OrdinalIgnoreCase))
            return "Phòng đang bảo trì, chưa thể đặt.";
        if (message.Contains("ORA-20123", StringComparison.OrdinalIgnoreCase))
            return "Phòng đang dọn hoặc chưa sẵn sàng cho ngày nhận phòng hôm nay.";
        if (message.Contains("ORA-20122", StringComparison.OrdinalIgnoreCase))
            return "Phòng chưa sẵn sàng để Check-in. Nhân viên cần hoàn tất dọn phòng trước.";
        if (message.Contains("ORA-20101", StringComparison.OrdinalIgnoreCase))
            return "Phòng đã có booking trùng khoảng ngày đã chọn. Vui lòng chọn phòng hoặc ngày khác.";
        if (message.Contains("ORA-20120", StringComparison.OrdinalIgnoreCase))
            return "Phòng không còn ở trạng thái Đang dọn.";
        if (message.Contains("ORA-20121", StringComparison.OrdinalIgnoreCase))
            return "Không tìm thấy phòng đã chọn.";
        if (message.Contains("ORA-20011", StringComparison.OrdinalIgnoreCase))
            return "Không tìm thấy booking.";
        return "Thao tác chưa thành công do điều kiện nghiệp vụ chưa hợp lệ. Vui lòng kiểm tra trạng thái phòng và booking.";
    }
}
