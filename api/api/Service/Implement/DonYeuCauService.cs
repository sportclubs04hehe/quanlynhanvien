using api.DTO;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;

namespace api.Service.Implement
{
    public class DonYeuCauService : IDonYeuCauService
    {
        private readonly IDonYeuCauRepository _donYeuCauRepo;
        private readonly INhanVienRepository _nhanVienRepo;
        private readonly IMapper _mapper;

        public DonYeuCauService(
            IDonYeuCauRepository donYeuCauRepo,
            INhanVienRepository nhanVienRepo,
            IMapper mapper)
        {
            _donYeuCauRepo = donYeuCauRepo;
            _nhanVienRepo = nhanVienRepo;
            _mapper = mapper;
        }

        #region CRUD Operations

        public async Task<PagedResult<DonYeuCauDto>> GetAllAsync(FilterDonYeuCauDto filter)
        {
            var (items, totalCount) = await _donYeuCauRepo.GetAllAsync(filter);
            var dtos = _mapper.Map<List<DonYeuCauDto>>(items);

            return new PagedResult<DonYeuCauDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<PagedResult<DonYeuCauDto>> GetProcessedDonsAsync(FilterDonYeuCauDto filter)
        {
            // Call GetAllAsync first
            var (items, totalCount) = await _donYeuCauRepo.GetAllAsync(filter);
            
            // Filter out DangChoDuyet (only show processed requests)
            var processedItems = items
                .Where(d => d.TrangThai != TrangThaiDon.DangChoDuyet)
                .ToList();
            
            var dtos = _mapper.Map<List<DonYeuCauDto>>(processedItems);

            return new PagedResult<DonYeuCauDto>
            {
                Items = dtos,
                TotalCount = processedItems.Count, // Adjust total count after filtering
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<DonYeuCauDto?> GetByIdAsync(Guid id)
        {
            var don = await _donYeuCauRepo.GetByIdAsync(id);
            return don == null ? null : _mapper.Map<DonYeuCauDto>(don);
        }

        public async Task<DonYeuCauDto> CreateAsync(Guid nhanVienId, CreateDonYeuCauDto dto)
        {
            // 1. Validate nhân viên tồn tại
            var nhanVien = await _nhanVienRepo.GetByIdAsync(nhanVienId);
            if (nhanVien == null)
                throw new InvalidOperationException("Không tìm thấy nhân viên");

            // 2. Validate theo loại đơn
            await ValidateCreateDonAsync(nhanVienId, dto);

            // 3. Map và set thông tin
            var don = _mapper.Map<DonYeuCau>(dto);
            don.NhanVienId = nhanVienId;
            
            // Chuyển đổi DateTime sang UTC
            ConvertDateTimesToUtc(don);

            // 4. Tạo đơn
            var created = await _donYeuCauRepo.CreateAsync(don);

            // 5. Return DTO
            return _mapper.Map<DonYeuCauDto>(created);
        }

        public async Task<DonYeuCauDto?> UpdateAsync(Guid donId, Guid nhanVienId, UpdateDonYeuCauDto dto)
        {
            // 1. Lấy đơn
            var don = await _donYeuCauRepo.GetByIdAsync(donId);
            if (don == null)
                return null;

            // 2. Kiểm tra ownership
            if (don.NhanVienId != nhanVienId)
                throw new UnauthorizedAccessException("Bạn không có quyền sửa đơn này");

            // 3. Kiểm tra trạng thái
            if (!await _donYeuCauRepo.CanEditAsync(donId))
                throw new InvalidOperationException("Chỉ có thể sửa đơn đang chờ duyệt");

            // 4. Validate theo loại đơn (excludeDonId để không check với chính đơn này)
            await ValidateUpdateDonAsync(nhanVienId, don.LoaiDon, dto, donId);

            // 5. Map và update
            _mapper.Map(dto, don);
            
            // Chuyển đổi DateTime sang UTC
            ConvertDateTimesToUtc(don);
            
            var updated = await _donYeuCauRepo.UpdateAsync(don);

            // 6. Return DTO
            return _mapper.Map<DonYeuCauDto>(updated);
        }

        public async Task<bool> DeleteAsync(Guid donId, Guid userId, bool isGiamDoc)
        {
            var don = await _donYeuCauRepo.GetByIdAsync(donId);
            if (don == null)
                return false;

            // Giám Đốc có thể xóa bất kỳ đơn nào
            // Nhân viên chỉ xóa được đơn của mình khi đang chờ duyệt
            if (!isGiamDoc)
            {
                if (don.NhanVienId != userId)
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa đơn này");

                if (don.TrangThai != TrangThaiDon.DangChoDuyet)
                    throw new InvalidOperationException("Chỉ có thể xóa đơn đang chờ duyệt");
            }

            return await _donYeuCauRepo.DeleteAsync(donId);
        }

        #endregion

        #region Business Operations

        public async Task<PagedResult<DonYeuCauDto>> GetMyDonsAsync(
            Guid nhanVienId,
            int pageNumber,
            int pageSize,
            LoaiDonYeuCau? loaiDon = null,
            TrangThaiDon? trangThai = null)
        {
            var (items, totalCount) = await _donYeuCauRepo.GetByNhanVienIdAsync(
                nhanVienId, pageNumber, pageSize, loaiDon, trangThai);

            var dtos = _mapper.Map<List<DonYeuCauDto>>(items);

            return new PagedResult<DonYeuCauDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<DonYeuCauDto>> GetDonCanDuyetAsync(
            Guid nguoiDuyetId,
            int pageNumber,
            int pageSize)
        {
            var (items, totalCount) = await _donYeuCauRepo.GetDonCanDuyetAsync(
                nguoiDuyetId, pageNumber, pageSize);

            var dtos = _mapper.Map<List<DonYeuCauDto>>(items);

            return new PagedResult<DonYeuCauDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<DonYeuCauDto>> GetByPhongBanAsync(
            Guid phongBanId,
            int pageNumber,
            int pageSize,
            TrangThaiDon? trangThai = null)
        {
            var (items, totalCount) = await _donYeuCauRepo.GetByPhongBanIdAsync(
                phongBanId, pageNumber, pageSize, trangThai);

            var dtos = _mapper.Map<List<DonYeuCauDto>>(items);

            return new PagedResult<DonYeuCauDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<DonYeuCauDto> ChapThuanDonAsync(Guid donId, Guid nguoiDuyetId, string? ghiChu = null)
        {
            // Validate người duyệt tồn tại
            var nguoiDuyet = await _nhanVienRepo.GetByIdAsync(nguoiDuyetId);
            if (nguoiDuyet == null)
                throw new InvalidOperationException("Không tìm thấy người duyệt");

            var don = await _donYeuCauRepo.DuyetDonAsync(
                donId, nguoiDuyetId, TrangThaiDon.DaChapThuan, ghiChu);

            return _mapper.Map<DonYeuCauDto>(don);
        }

        public async Task<DonYeuCauDto> TuChoiDonAsync(Guid donId, Guid nguoiDuyetId, string ghiChu)
        {
            // Validate ghi chú bắt buộc khi từ chối
            if (string.IsNullOrWhiteSpace(ghiChu))
                throw new InvalidOperationException("Vui lòng nhập lý do từ chối");

            // Validate người duyệt tồn tại
            var nguoiDuyet = await _nhanVienRepo.GetByIdAsync(nguoiDuyetId);
            if (nguoiDuyet == null)
                throw new InvalidOperationException("Không tìm thấy người duyệt");

            var don = await _donYeuCauRepo.DuyetDonAsync(
                donId, nguoiDuyetId, TrangThaiDon.BiTuChoi, ghiChu);

            return _mapper.Map<DonYeuCauDto>(don);
        }

        public async Task<bool> HuyDonAsync(Guid donId, Guid nhanVienId)
        {
            return await _donYeuCauRepo.HuyDonAsync(donId, nhanVienId);
        }

        #endregion

        #region Statistics

        public async Task<ThongKeDonYeuCauDto> ThongKeMyDonsAsync(
            Guid nhanVienId, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            return await _donYeuCauRepo.ThongKeByNhanVienAsync(nhanVienId, fromDate, toDate);
        }

        public async Task<ThongKeDonYeuCauDto> ThongKePhongBanAsync(
            Guid phongBanId, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            return await _donYeuCauRepo.ThongKeByPhongBanAsync(phongBanId, fromDate, toDate);
        }

        public async Task<ThongKeDonYeuCauDto> ThongKeToanCongTyAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            return await _donYeuCauRepo.ThongKeToanCongTyAsync(fromDate, toDate);
        }

        public async Task<int> CountDonChoDuyetAsync(Guid nguoiDuyetId)
        {
            return await _donYeuCauRepo.CountDonChoDuyetAsync(nguoiDuyetId);
        }

        #endregion

        #region Private Validation Methods

        private async Task ValidateCreateDonAsync(Guid nhanVienId, CreateDonYeuCauDto dto)
        {
            switch (dto.LoaiDon)
            {
                case LoaiDonYeuCau.NghiPhep:
                    ValidateNghiPhep(dto);
                    // Kiểm tra trùng ngày nghỉ
                    if (dto.NgayBatDau.HasValue && dto.NgayKetThuc.HasValue)
                    {
                        var isTrung = await _donYeuCauRepo.KiemTraTrungNgayNghiAsync(
                            nhanVienId, dto.NgayBatDau.Value, dto.NgayKetThuc.Value);
                        if (isTrung)
                            throw new InvalidOperationException("Đã có đơn nghỉ phép trong khoảng thời gian này");
                    }
                    break;

                case LoaiDonYeuCau.LamThemGio:
                    ValidateLamThemGio(dto);
                    break;

                case LoaiDonYeuCau.DiMuon:
                    ValidateDiMuon(dto);
                    // Kiểm tra đã có đơn đi muộn trong ngày chưa
                    if (dto.NgayDiMuon.HasValue)
                    {
                        var daCoDon = await _donYeuCauRepo.DaCoDoiDiMuonTrongNgayAsync(
                            nhanVienId, dto.NgayDiMuon.Value);
                        if (daCoDon)
                            throw new InvalidOperationException("Đã có đơn đi muộn trong ngày này");
                    }
                    break;

                case LoaiDonYeuCau.CongTac:
                    ValidateCongTac(dto);
                    break;
            }
        }

        private async Task ValidateUpdateDonAsync(
            Guid nhanVienId, 
            LoaiDonYeuCau loaiDon, 
            UpdateDonYeuCauDto dto, 
            Guid excludeDonId)
        {
            switch (loaiDon)
            {
                case LoaiDonYeuCau.NghiPhep:
                    ValidateNghiPhep(dto);
                    if (dto.NgayBatDau.HasValue && dto.NgayKetThuc.HasValue)
                    {
                        var isTrung = await _donYeuCauRepo.KiemTraTrungNgayNghiAsync(
                            nhanVienId, dto.NgayBatDau.Value, dto.NgayKetThuc.Value, excludeDonId);
                        if (isTrung)
                            throw new InvalidOperationException("Đã có đơn nghỉ phép trong khoảng thời gian này");
                    }
                    break;

                case LoaiDonYeuCau.LamThemGio:
                    ValidateLamThemGio(dto);
                    break;

                case LoaiDonYeuCau.DiMuon:
                    ValidateDiMuon(dto);
                    if (dto.NgayDiMuon.HasValue)
                    {
                        var daCoDon = await _donYeuCauRepo.DaCoDoiDiMuonTrongNgayAsync(
                            nhanVienId, dto.NgayDiMuon.Value, excludeDonId);
                        if (daCoDon)
                            throw new InvalidOperationException("Đã có đơn đi muộn trong ngày này");
                    }
                    break;

                case LoaiDonYeuCau.CongTac:
                    ValidateCongTac(dto);
                    break;
            }
        }

        private void ValidateNghiPhep(CreateDonYeuCauDto dto)
        {
            if (!dto.NgayBatDau.HasValue)
                throw new InvalidOperationException("Ngày bắt đầu là bắt buộc cho đơn nghỉ phép");
            if (!dto.NgayKetThuc.HasValue)
                throw new InvalidOperationException("Ngày kết thúc là bắt buộc cho đơn nghỉ phép");
            if (dto.NgayBatDau.Value > dto.NgayKetThuc.Value)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");
            if (dto.NgayBatDau.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Không thể tạo đơn nghỉ phép cho ngày trong quá khứ");
        }

        private void ValidateNghiPhep(UpdateDonYeuCauDto dto)
        {
            if (!dto.NgayBatDau.HasValue)
                throw new InvalidOperationException("Ngày bắt đầu là bắt buộc cho đơn nghỉ phép");
            if (!dto.NgayKetThuc.HasValue)
                throw new InvalidOperationException("Ngày kết thúc là bắt buộc cho đơn nghỉ phép");
            if (dto.NgayBatDau.Value > dto.NgayKetThuc.Value)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");
        }

        private void ValidateLamThemGio(CreateDonYeuCauDto dto)
        {
            if (!dto.SoGioLamThem.HasValue)
                throw new InvalidOperationException("Số giờ làm thêm là bắt buộc");
            if (!dto.NgayLamThem.HasValue)
                throw new InvalidOperationException("Ngày làm thêm là bắt buộc");
            if (dto.SoGioLamThem.Value <= 0 || dto.SoGioLamThem.Value > 24)
                throw new InvalidOperationException("Số giờ làm thêm phải từ 0.5 đến 24 giờ");
        }

        private void ValidateLamThemGio(UpdateDonYeuCauDto dto)
        {
            if (!dto.SoGioLamThem.HasValue)
                throw new InvalidOperationException("Số giờ làm thêm là bắt buộc");
            if (!dto.NgayLamThem.HasValue)
                throw new InvalidOperationException("Ngày làm thêm là bắt buộc");
            if (dto.SoGioLamThem.Value <= 0 || dto.SoGioLamThem.Value > 24)
                throw new InvalidOperationException("Số giờ làm thêm phải từ 0.5 đến 24 giờ");
        }

        private void ValidateDiMuon(CreateDonYeuCauDto dto)
        {
            if (!dto.GioDuKienDen.HasValue)
                throw new InvalidOperationException("Giờ dự kiến đến là bắt buộc");
            if (!dto.NgayDiMuon.HasValue)
                throw new InvalidOperationException("Ngày đi muộn là bắt buộc");
            if (dto.NgayDiMuon.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Không thể tạo đơn đi muộn cho ngày trong quá khứ");
        }

        private void ValidateDiMuon(UpdateDonYeuCauDto dto)
        {
            if (!dto.GioDuKienDen.HasValue)
                throw new InvalidOperationException("Giờ dự kiến đến là bắt buộc");
            if (!dto.NgayDiMuon.HasValue)
                throw new InvalidOperationException("Ngày đi muộn là bắt buộc");
        }

        private void ValidateCongTac(CreateDonYeuCauDto dto)
        {
            if (!dto.NgayBatDau.HasValue)
                throw new InvalidOperationException("Ngày bắt đầu là bắt buộc cho đơn công tác");
            if (!dto.NgayKetThuc.HasValue)
                throw new InvalidOperationException("Ngày kết thúc là bắt buộc cho đơn công tác");
            if (string.IsNullOrWhiteSpace(dto.DiaDiemCongTac))
                throw new InvalidOperationException("Địa điểm công tác là bắt buộc");
            if (string.IsNullOrWhiteSpace(dto.MucDichCongTac))
                throw new InvalidOperationException("Mục đích công tác là bắt buộc");
            if (dto.NgayBatDau.Value > dto.NgayKetThuc.Value)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");
        }

        private void ValidateCongTac(UpdateDonYeuCauDto dto)
        {
            if (!dto.NgayBatDau.HasValue)
                throw new InvalidOperationException("Ngày bắt đầu là bắt buộc cho đơn công tác");
            if (!dto.NgayKetThuc.HasValue)
                throw new InvalidOperationException("Ngày kết thúc là bắt buộc cho đơn công tác");
            if (string.IsNullOrWhiteSpace(dto.DiaDiemCongTac))
                throw new InvalidOperationException("Địa điểm công tác là bắt buộc");
            if (string.IsNullOrWhiteSpace(dto.MucDichCongTac))
                throw new InvalidOperationException("Mục đích công tác là bắt buộc");
            if (dto.NgayBatDau.Value > dto.NgayKetThuc.Value)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");
        }

        /// <summary>
        /// Chuyển đổi tất cả DateTime fields sang UTC để tránh lỗi PostgreSQL
        /// </summary>
        private void ConvertDateTimesToUtc(DonYeuCau don)
        {
            // Chuyển các DateTime nullable sang UTC nếu có giá trị
            if (don.NgayBatDau.HasValue)
                don.NgayBatDau = DateTime.SpecifyKind(don.NgayBatDau.Value, DateTimeKind.Utc);

            if (don.NgayKetThuc.HasValue)
                don.NgayKetThuc = DateTime.SpecifyKind(don.NgayKetThuc.Value, DateTimeKind.Utc);

            if (don.NgayLamThem.HasValue)
                don.NgayLamThem = DateTime.SpecifyKind(don.NgayLamThem.Value, DateTimeKind.Utc);

            if (don.GioDuKienDen.HasValue)
                don.GioDuKienDen = DateTime.SpecifyKind(don.GioDuKienDen.Value, DateTimeKind.Utc);

            if (don.NgayDiMuon.HasValue)
                don.NgayDiMuon = DateTime.SpecifyKind(don.NgayDiMuon.Value, DateTimeKind.Utc);

            if (don.NgayDuyet.HasValue)
                don.NgayDuyet = DateTime.SpecifyKind(don.NgayDuyet.Value, DateTimeKind.Utc);

            if (don.NgayCapNhat.HasValue)
                don.NgayCapNhat = DateTime.SpecifyKind(don.NgayCapNhat.Value, DateTimeKind.Utc);

            // NgayTao luôn là UTC từ default value trong model
            don.NgayTao = DateTime.SpecifyKind(don.NgayTao, DateTimeKind.Utc);
        }

        #endregion
    }
}

