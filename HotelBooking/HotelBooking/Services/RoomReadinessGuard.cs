using Oracle.ManagedDataAccess.Client;

namespace HotelBooking.Services;

/// <summary>
/// Bảo vệ luồng tạo booking trong ngày; đặt trước tương lai vẫn được phép
/// khi phòng CLEANING/OCCUPIED nhưng không có booking trùng ngày.
/// Điều kiện này được kiểm tra lại tại server thay vì chỉ dựa trên trang tìm phòng.
/// </summary>
public static class RoomReadinessGuard
{
    public static async Task<(bool Allowed, string Message)> CheckForBookingAsync(
        OracleConnection connection, OracleTransaction transaction, int roomId, DateTime checkIn)
    {
        if (checkIn.Date < DateTime.Today)
            return (false, "Ngày nhận phòng không được ở trong quá khứ.");

        // Khóa phòng trong suốt transaction tạo booking nhằm tránh housekeeping
        // đồng thời chuyển trạng thái trong lúc server đang kiểm tra.
        const string sql = @"SELECT STATUS
                             FROM ROOMS
                             WHERE ROOM_ID = :P_ROOM_ID
                             FOR UPDATE WAIT 5";
        await using var command = new OracleCommand(sql, connection)
        {
            BindByName = true,
            Transaction = transaction
        };
        command.Parameters.Add("P_ROOM_ID", OracleDbType.Int32).Value = roomId;
        var raw = await command.ExecuteScalarAsync();
        if (raw is null || raw == DBNull.Value)
            return (false, "Không tìm thấy phòng đã chọn.");

        var status = Convert.ToString(raw)?.Trim().ToUpperInvariant() ?? "";
        if (status == "MAINTENANCE")
            return (false, "Phòng đang bảo trì nên chưa thể đặt.");

        if (checkIn.Date <= DateTime.Today && status != "AVAILABLE")
        {
            return status switch
            {
                "CLEANING" => (false, "Phòng đang dọn nên chưa thể nhận khách hôm nay. Vui lòng chọn ngày khác hoặc chờ nhân viên xác nhận dọn xong."),
                "OCCUPIED" => (false, "Phòng đang có khách, chưa sẵn sàng nhận phòng hôm nay."),
                _ => (false, "Phòng chưa sẵn sàng nhận khách hôm nay.")
            };
        }

        // Trạng thái hiện tại không thay thế kiểm tra trùng lịch; Package
        // SP_CREATE_BOOKING/trigger vẫn chịu trách nhiệm kiểm tra trùng booking.
        return (true, "OK");
    }
}
