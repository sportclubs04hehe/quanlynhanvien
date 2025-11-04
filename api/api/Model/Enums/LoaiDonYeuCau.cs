namespace api.Model.Enums
{
    /// <summary>
    /// Enum định nghĩa các loại đơn yêu cầu trong hệ thống
    /// </summary>
    public enum LoaiDonYeuCau
    {
        NghiPhep = 1,       // Đơn xin nghỉ phép (có lương)
        LamThemGio = 2,     // Đơn xin làm thêm giờ (overtime)
        DiMuon = 3,         // Đơn xin đi muộn
        CongTac = 4         // Đơn xin đi công tác
    }

    /// <summary>
    /// Extension methods cho LoaiDonYeuCau
    /// </summary>
    public static class LoaiDonYeuCauExtensions
    {
        /// <summary>
        /// Lấy tên hiển thị tiếng Việt
        /// </summary>
        public static string ToDisplayName(this LoaiDonYeuCau loai)
        {
            return loai switch
            {
                LoaiDonYeuCau.NghiPhep => "Nghỉ Phép",
                LoaiDonYeuCau.LamThemGio => "Làm Thêm Giờ",
                LoaiDonYeuCau.DiMuon => "Đi Muộn",
                LoaiDonYeuCau.CongTac => "Công Tác",
                _ => "Unknown"
            };
        }
    }
}
