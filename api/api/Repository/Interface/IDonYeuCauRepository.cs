using api.DTO;
using api.Model;
using api.Model.Enums;

namespace api.Repository.Interface
{
    public interface IDonYeuCauRepository
    {
        #region CRUD cơ bản

        /// <summary>
        /// Lấy danh sách đơn yêu cầu với filter và phân trang
        /// </summary>
        Task<(List<DonYeuCau> Items, int TotalCount)> GetAllAsync(FilterDonYeuCauDto filter);

        /// <summary>
        /// Lấy đơn yêu cầu theo ID
        /// </summary>
        Task<DonYeuCau?> GetByIdAsync(Guid id);

        /// <summary>
        /// Tạo đơn yêu cầu mới
        /// </summary>
        Task<DonYeuCau> CreateAsync(DonYeuCau donYeuCau);

        /// <summary>
        /// Cập nhật đơn yêu cầu (chỉ cho đơn đang chờ duyệt)
        /// </summary>
        Task<DonYeuCau> UpdateAsync(DonYeuCau donYeuCau);

        /// <summary>
        /// Xóa đơn yêu cầu
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Kiểm tra đơn có tồn tại không
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        #endregion

        #region Business Logic Methods

        /// <summary>
        /// Lấy danh sách đơn của một nhân viên
        /// </summary>
        Task<(List<DonYeuCau> Items, int TotalCount)> GetByNhanVienIdAsync(
            Guid nhanVienId, 
            int pageNumber, 
            int pageSize,
            string? maDon = null,
            string? lyDo = null,
            LoaiDonYeuCau? loaiDon = null,
            TrangThaiDon? trangThai = null);

        /// <summary>
        /// Lấy danh sách đơn cần người duyệt xử lý
        /// (Đơn của nhân viên trong phòng ban hoặc đơn cần Giám Đốc/Trưởng Phòng duyệt)
        /// </summary>
        Task<(List<DonYeuCau> Items, int TotalCount)> GetDonCanDuyetAsync(
            Guid nguoiDuyetId, 
            int pageNumber, 
            int pageSize);

        /// <summary>
        /// Lấy danh sách đơn theo phòng ban
        /// </summary>
        Task<(List<DonYeuCau> Items, int TotalCount)> GetByPhongBanIdAsync(
            Guid phongBanId, 
            int pageNumber, 
            int pageSize,
            TrangThaiDon? trangThai = null);

        /// <summary>
        /// Lấy danh sách đơn theo trạng thái
        /// </summary>
        Task<List<DonYeuCau>> GetByTrangThaiAsync(TrangThaiDon trangThai);

        /// <summary>
        /// Duyệt/Từ chối đơn
        /// </summary>
        Task<DonYeuCau> DuyetDonAsync(Guid donId, Guid nguoiDuyetId, TrangThaiDon trangThai, string? ghiChu);

        /// <summary>
        /// Hủy đơn (chỉ nhân viên tạo đơn mới được hủy, và chỉ hủy được đơn đang chờ duyệt)
        /// </summary>
        Task<bool> HuyDonAsync(Guid donId, Guid nhanVienId);

        /// <summary>
        /// Kiểm tra nhân viên có phải chủ đơn không
        /// </summary>
        Task<bool> IsOwnerAsync(Guid donId, Guid nhanVienId);

        /// <summary>
        /// Kiểm tra đơn có thể chỉnh sửa không (đang chờ duyệt)
        /// </summary>
        Task<bool> CanEditAsync(Guid donId);

        /// <summary>
        /// Kiểm tra đơn có thể hủy không (đang chờ duyệt)
        /// </summary>
        Task<bool> CanCancelAsync(Guid donId);

        /// <summary>
        /// Lấy đơn nghỉ sắp tới (approved, chưa đến ngày)
        /// </summary>
        Task<List<DonYeuCau>> GetUpcomingDonsAsync(Guid nhanVienId, int limit = 5);

        /// <summary>
        /// Lấy đơn theo khoảng thời gian và trạng thái
        /// </summary>
        Task<List<DonYeuCau>> GetDonsByDateRangeAsync(
            Guid nhanVienId, 
            DateTime startDate, 
            DateTime endDate, 
            TrangThaiDon? trangThai = null);

        /// <summary>
        /// Lấy đơn theo loại đơn và tháng (dùng cho đơn làm thêm giờ)
        /// </summary>
        Task<List<DonYeuCau>> GetDonsByLoaiDonAsync(
            Guid nhanVienId,
            LoaiDonYeuCau loaiDon,
            TrangThaiDon? trangThai,
            int nam,
            int thang);

        #endregion

        #region Thống kê

        /// <summary>
        /// Thống kê đơn yêu cầu theo nhân viên
        /// </summary>
        Task<ThongKeDonYeuCauDto> ThongKeByNhanVienAsync(Guid nhanVienId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Thống kê đơn yêu cầu theo phòng ban
        /// </summary>
        Task<ThongKeDonYeuCauDto> ThongKeByPhongBanAsync(Guid phongBanId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Thống kê đơn yêu cầu toàn công ty
        /// </summary>
        Task<ThongKeDonYeuCauDto> ThongKeToanCongTyAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Đếm số đơn đang chờ duyệt của một người duyệt
        /// </summary>
        Task<int> CountDonChoDuyetAsync(Guid nguoiDuyetId);

        #endregion

        #region Validation

        /// <summary>
        /// Đếm số đơn theo loại và năm (để sinh mã đơn tự động)
        /// </summary>
        Task<int> CountByLoaiAndYearAsync(LoaiDonYeuCau loaiDon, int year);

        /// <summary>
        /// Kiểm tra trùng đơn nghỉ phép (cùng khoảng thời gian)
        /// </summary>
        Task<bool> KiemTraTrungNgayNghiAsync(Guid nhanVienId, DateTime ngayBatDau, DateTime ngayKetThuc, Guid? excludeDonId = null);

        /// <summary>
        /// Kiểm tra xung đột nghỉ phép với logic theo LoaiNghiPhep
        /// - Buổi sáng/chiều: Cho phép cùng ngày nếu khác buổi
        /// - Một ngày: Xung đột với mọi loại cùng ngày
        /// - Nhiều ngày: Xung đột với mọi loại trong khoảng thời gian
        /// </summary>
        Task<bool> KiemTraXungDotNghiPhepAsync(
            Guid nhanVienId, 
            DateTime ngayBatDau, 
            DateTime ngayKetThuc, 
            LoaiNghiPhep loaiNghiPhep,
            Guid? excludeDonId = null);

        /// <summary>
        /// Kiểm tra đã có đơn đi muộn trong ngày chưa
        /// </summary>
        Task<bool> DaCoDoiDiMuonTrongNgayAsync(Guid nhanVienId, DateTime ngay, Guid? excludeDonId = null);

        #endregion
    }
}

