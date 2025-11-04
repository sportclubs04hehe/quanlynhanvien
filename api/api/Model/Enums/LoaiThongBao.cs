namespace api.Model.Enums
{
    /// <summary>
    /// Enum định nghĩa các loại thông báo trong hệ thống
    /// </summary>
    public enum LoaiThongBao
    {
        /// <summary>
        /// Thông báo liên quan đến đơn yêu cầu
        /// VD: Đơn được duyệt, đơn bị từ chối, có đơn mới cần duyệt
        /// </summary>
        DonYeuCau = 1,

        /// <summary>
        /// Thông báo chung từ công ty
        /// VD: Thông báo nghỉ lễ, sự kiện, chính sách mới
        /// </summary>
        ThongBaoChung = 2,

        /// <summary>
        /// Thông báo về nhân sự
        /// VD: Chào mừng nhân viên mới, thay đổi tổ chức
        /// </summary>
        NhanSu = 3,

        /// <summary>
        /// Thông báo hệ thống
        /// VD: Bảo trì, cập nhật tính năng
        /// </summary>
        HeThong = 4,

        /// <summary>
        /// Nhắc nhở
        /// VD: Nhắc chấm công, nhắc nộp báo cáo
        /// </summary>
        NhacNho = 5
    }

    /// <summary>
    /// Extension methods cho LoaiThongBao
    /// </summary>
    public static class LoaiThongBaoExtensions
    {
        public const string DonYeuCau = "DON_YEU_CAU";
        public const string ThongBaoChung = "THONG_BAO_CHUNG";
        public const string NhanSu = "NHAN_SU";
        public const string HeThong = "HE_THONG";
        public const string NhacNho = "NHAC_NHO";

        /// <summary>
        /// Lấy string constant từ enum
        /// </summary>
        public static string ToConstant(this LoaiThongBao loai)
        {
            return loai switch
            {
                LoaiThongBao.DonYeuCau => DonYeuCau,
                LoaiThongBao.ThongBaoChung => ThongBaoChung,
                LoaiThongBao.NhanSu => NhanSu,
                LoaiThongBao.HeThong => HeThong,
                LoaiThongBao.NhacNho => NhacNho,
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Lấy tên hiển thị tiếng Việt
        /// </summary>
        public static string ToDisplayName(this LoaiThongBao loai)
        {
            return loai switch
            {
                LoaiThongBao.DonYeuCau => "Đơn Yêu Cầu",
                LoaiThongBao.ThongBaoChung => "Thông Báo Chung",
                LoaiThongBao.NhanSu => "Nhân Sự",
                LoaiThongBao.HeThong => "Hệ Thống",
                LoaiThongBao.NhacNho => "Nhắc Nhở",
                _ => "Unknown"
            };
        }
    }
}
