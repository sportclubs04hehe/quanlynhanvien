using api.DTO;
using api.Model.Enums;

namespace api.Service.Interface
{
    public interface IDonYeuCauService
    {
        #region CRUD Operations

        /// <summary>
        /// Lấy danh sách đơn yêu cầu với filter và phân trang
        /// </summary>
        Task<PagedResult<DonYeuCauDto>> GetAllAsync(FilterDonYeuCauDto filter);

        /// <summary>
        /// Lấy đơn yêu cầu theo ID
        /// </summary>
        Task<DonYeuCauDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// Tạo đơn yêu cầu mới
        /// </summary>
        Task<DonYeuCauDto> CreateAsync(Guid nhanVienId, CreateDonYeuCauDto dto);

        /// <summary>
        /// Cập nhật đơn yêu cầu (chỉ owner và đơn đang chờ duyệt)
        /// </summary>
        Task<DonYeuCauDto?> UpdateAsync(Guid donId, Guid nhanVienId, UpdateDonYeuCauDto dto);

        /// <summary>
        /// Xóa đơn yêu cầu (chỉ Giám Đốc hoặc owner khi đơn chưa duyệt)
        /// </summary>
        Task<bool> DeleteAsync(Guid donId, Guid userId, bool isGiamDoc);

        #endregion

        #region Business Operations

        /// <summary>
        /// Lấy danh sách đơn của nhân viên hiện tại
        /// </summary>
        Task<PagedResult<DonYeuCauDto>> GetMyDonsAsync(
            Guid nhanVienId,
            int pageNumber,
            int pageSize,
            LoaiDonYeuCau? loaiDon = null,
            TrangThaiDon? trangThai = null);

        /// <summary>
        /// Lấy danh sách đơn cần duyệt (cho Giám Đốc/Trưởng Phòng)
        /// </summary>
        Task<PagedResult<DonYeuCauDto>> GetDonCanDuyetAsync(
            Guid nguoiDuyetId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Lấy danh sách đơn theo phòng ban (cho Trưởng Phòng)
        /// </summary>
        Task<PagedResult<DonYeuCauDto>> GetByPhongBanAsync(
            Guid phongBanId,
            int pageNumber,
            int pageSize,
            TrangThaiDon? trangThai = null);

        /// <summary>
        /// Duyệt đơn yêu cầu
        /// </summary>
        Task<DonYeuCauDto> ChapThuanDonAsync(Guid donId, Guid nguoiDuyetId, string? ghiChu = null);

        /// <summary>
        /// Từ chối đơn yêu cầu
        /// </summary>
        Task<DonYeuCauDto> TuChoiDonAsync(Guid donId, Guid nguoiDuyetId, string ghiChu);

        /// <summary>
        /// Hủy đơn (chỉ owner và đơn đang chờ duyệt)
        /// </summary>
        Task<bool> HuyDonAsync(Guid donId, Guid nhanVienId);

        #endregion

        #region Statistics

        /// <summary>
        /// Thống kê đơn yêu cầu của nhân viên
        /// </summary>
        Task<ThongKeDonYeuCauDto> ThongKeMyDonsAsync(Guid nhanVienId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Thống kê đơn yêu cầu theo phòng ban
        /// </summary>
        Task<ThongKeDonYeuCauDto> ThongKePhongBanAsync(Guid phongBanId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Thống kê đơn yêu cầu toàn công ty (chỉ Giám Đốc)
        /// </summary>
        Task<ThongKeDonYeuCauDto> ThongKeToanCongTyAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Đếm số đơn đang chờ duyệt của người duyệt
        /// </summary>
        Task<int> CountDonChoDuyetAsync(Guid nguoiDuyetId);

        #endregion
    }
}

