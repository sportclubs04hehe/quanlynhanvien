using api.Model;

namespace api.Repository.Interface
{
    public interface INghiPhepQuotaRepository
    {
        /// <summary>
        /// Lấy quota của nhân viên theo tháng, năm
        /// </summary>
        Task<NghiPhepQuota?> GetByNhanVienAndMonthAsync(Guid nhanVienId, int nam, int thang);

        /// <summary>
        /// Lấy tất cả quota của nhân viên trong năm
        /// </summary>
        Task<List<NghiPhepQuota>> GetByNhanVienAndYearAsync(Guid nhanVienId, int nam);

        /// <summary>
        /// Tạo quota mới
        /// </summary>
        Task<NghiPhepQuota> CreateAsync(NghiPhepQuota quota);

        /// <summary>
        /// Cập nhật quota
        /// </summary>
        Task<NghiPhepQuota> UpdateAsync(NghiPhepQuota quota);

        /// <summary>
        /// Xóa quota
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Lấy hoặc tạo quota cho tháng (nếu chưa có)
        /// </summary>
        Task<NghiPhepQuota> GetOrCreateQuotaAsync(Guid nhanVienId, int nam, int thang, decimal soNgayPhepThang = 1m);

        /// <summary>
        /// Cập nhật số ngày đã sử dụng và số giờ làm thêm
        /// </summary>
        Task RecalculateQuotaAsync(Guid nhanVienId, int nam, int thang);

        /// <summary>
        /// Lấy danh sách quota của nhiều nhân viên (cho Giám Đốc xem tổng quan)
        /// </summary>
        Task<List<NghiPhepQuota>> GetQuotasByMonthAsync(int nam, int thang, Guid? phongBanId = null);

        /// <summary>
        /// Lấy danh sách nhân viên cho bulk operation
        /// </summary>
        Task<List<NhanVien>> GetNhanViensForBulkAsync(Guid? phongBanId = null);
    }
}
