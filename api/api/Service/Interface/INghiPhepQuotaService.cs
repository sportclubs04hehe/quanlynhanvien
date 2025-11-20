using api.DTO;

namespace api.Service.Interface
{
    public interface INghiPhepQuotaService
    {
        /// <summary>
        /// Lấy quota của nhân viên theo tháng, tự động tạo nếu chưa có
        /// </summary>
        Task<NghiPhepQuotaDto> GetOrCreateQuotaAsync(Guid nhanVienId, int nam, int thang);

        /// <summary>
        /// Lấy dashboard "Lịch Nghỉ & Công Việc" cho nhân viên
        /// </summary>
        Task<LichNghiDashboardDto> GetLichNghiDashboardAsync(Guid nhanVienId, int? nam = null, int? thang = null);

        /// <summary>
        /// Lấy calendar view theo tháng
        /// </summary>
        Task<LichNghiCalendarDto> GetCalendarAsync(Guid nhanVienId, int nam, int thang);

        /// <summary>
        /// Cập nhật quota (Giám Đốc thay đổi số ngày phép cho nhân viên)
        /// </summary>
        Task<NghiPhepQuotaDto> UpdateQuotaAsync(Guid quotaId, UpsertNghiPhepQuotaDto dto);

        /// <summary>
        /// Tạo quota cho tháng mới (thường gọi tự động)
        /// </summary>
        Task<NghiPhepQuotaDto> CreateQuotaAsync(UpsertNghiPhepQuotaDto dto);

        /// <summary>
        /// Recalculate quota sau khi approve/reject đơn
        /// </summary>
        Task RecalculateQuotaAsync(Guid nhanVienId, int nam, int thang);

        /// <summary>
        /// Lấy danh sách quota của tất cả nhân viên trong tháng (Giám Đốc)
        /// </summary>
        Task<List<NghiPhepQuotaDto>> GetQuotasByMonthAsync(int nam, int thang, Guid? phongBanId = null);

        /// <summary>
        /// Kiểm tra nhân viên có đủ quota để tạo đơn nghỉ không
        /// </summary>
        Task<(bool IsValid, string? Message)> ValidateQuotaAsync(Guid nhanVienId, DateTime ngayBatDau, DateTime ngayKetThuc, decimal soNgayNghi);
    }
}
