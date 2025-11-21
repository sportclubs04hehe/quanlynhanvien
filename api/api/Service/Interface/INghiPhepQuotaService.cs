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
        /// Recalculate quota sau khi approve/reject đơn
        /// </summary>
        Task RecalculateQuotaAsync(Guid nhanVienId, int nam, int thang);

        /// <summary>
        /// Lấy danh sách quota của tất cả nhân viên trong tháng (Giám Đốc)
        /// </summary>
        Task<List<NghiPhepQuotaDto>> GetQuotasByMonthAsync(int nam, int thang, Guid? phongBanId = null);

        /// <summary>
        /// Bulk create hoặc update quota cho nhiều nhân viên cùng lúc
        /// </summary>
        Task<BulkQuotaResultDto> BulkCreateOrUpdateQuotaAsync(BulkQuotaRequestDto request);

    }
}
