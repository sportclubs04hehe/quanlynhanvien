namespace api.Model.Enums
{
    /// <summary>
    /// Enum định nghĩa trạng thái của đơn yêu cầu
    /// </summary>
    public enum TrangThaiDon
    {
        DangChoDuyet = 1,   // Đơn mới tạo, đang chờ duyệt
        DaChapThuan = 2,    // Đã được phê duyệt
        BiTuChoi = 3,       // Bị từ chối
        DaHuy = 4           // Nhân viên tự hủy đơn
    }

    /// <summary>
    /// Extension methods cho TrangThaiDon
    /// </summary>
    public static class TrangThaiDonExtensions
    {
        /// <summary>
        /// Lấy tên hiển thị tiếng Việt
        /// </summary>
        public static string ToDisplayName(this TrangThaiDon trangThai)
        {
            return trangThai switch
            {
                TrangThaiDon.DangChoDuyet => "Đang Chờ Duyệt",
                TrangThaiDon.DaChapThuan => "Đã Chấp Thuận",
                TrangThaiDon.BiTuChoi => "Bị Từ Chối",
                TrangThaiDon.DaHuy => "Đã Hủy",
                _ => "Unknown"
            };
        }
    }
}
