using api.DTO;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;
using System.Text.Json;

namespace api.Service.Implement
{
    public class DonYeuCauService : IDonYeuCauService
    {
        private readonly IDonYeuCauRepository _donYeuCauRepo;
        private readonly INhanVienRepository _nhanVienRepo;
        private readonly IMapper _mapper;
        private readonly ITelegramService _telegramService;
        private readonly ILogger<DonYeuCauService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DonYeuCauService(
            IDonYeuCauRepository donYeuCauRepo,
            INhanVienRepository nhanVienRepo,
            IMapper mapper,
            ITelegramService telegramService,
            ILogger<DonYeuCauService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _donYeuCauRepo = donYeuCauRepo;
            _nhanVienRepo = nhanVienRepo;
            _mapper = mapper;
            _telegramService = telegramService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        #region CRUD Operations

        public async Task<PagedResult<DonYeuCauDto>> GetAllAsync(FilterDonYeuCauDto filter, Guid currentUserId)
        {
            // Auto-detect role: Nếu là Trưởng Phòng thì tự động filter theo phòng ban
            var currentUser = await _nhanVienRepo.GetByIdAsync(currentUserId);
            if (currentUser != null && currentUser.PhongBanId.HasValue)
            {
                // Kiểm tra có phải Trưởng Phòng không
                // Nếu user có phòng ban và filter chưa set phongBanId 
                // => Giả định là Trưởng Phòng (vì endpoint đã check role ở Controller)
                // Giám Đốc thường không có phongBanId hoặc có thể override filter
                if (!filter.PhongBanId.HasValue)
                {
                    // Tự động set phòng ban filter cho Trưởng Phòng
                    filter.PhongBanId = currentUser.PhongBanId.Value;
                }
            }

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
            
            // Sinh mã đơn tự động
            don.MaDon = await GenerateMaDonAsync(dto.LoaiDon);
            
            // Chuyển đổi DateTime sang UTC
            ConvertDateTimesToUtc(don);

            // 4. Tạo đơn
            var created = await _donYeuCauRepo.CreateAsync(don);

            // 5. Gửi thông báo Telegram (fire-and-forget với scope riêng)
            var donId = created.Id;
            var nhanVienId_ForTask = nhanVien.Id;
            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
                var nhanVienRepo = scope.ServiceProvider.GetRequiredService<INhanVienRepository>();
                var donYeuCauRepo = scope.ServiceProvider.GetRequiredService<IDonYeuCauRepository>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DonYeuCauService>>();

                try
                {
                    var donData = await donYeuCauRepo.GetByIdAsync(donId);
                    var nhanVienData = await nhanVienRepo.GetByIdAsync(nhanVienId_ForTask);

                    if (donData != null && nhanVienData != null)
                    {
                        await GuiThongBaoTelegramWithScopeAsync(donData, nhanVienData, 
                            telegramService, donYeuCauRepo, logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ [BACKGROUND] Lỗi gửi thông báo Telegram cho đơn ID: {DonId}", donId);
                }
            });

            // 6. Return DTO
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

            // CRITICAL: Không được xóa đơn đã chấp thuận hoặc bị từ chối (Audit compliance)
            if (don.TrangThai == TrangThaiDon.DaChapThuan)
            {
                throw new InvalidOperationException(
                    "Không thể xóa đơn đã được chấp thuận. Đây là chứng từ có giá trị pháp lý và cần được lưu trữ theo quy định.");
            }

            if (don.TrangThai == TrangThaiDon.BiTuChoi)
            {
                throw new InvalidOperationException(
                    "Không thể xóa đơn đã bị từ chối. Lịch sử này cần được lưu trữ để audit và phân tích.");
            }

            // Nhân viên: Chỉ xóa được đơn của mình khi DangChoDuyet hoặc DaHuy
            if (!isGiamDoc)
            {
                if (don.NhanVienId != userId)
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa đơn này");

                // Nhân viên chỉ xóa đơn chưa duyệt hoặc đã hủy
                if (don.TrangThai != TrangThaiDon.DangChoDuyet && 
                    don.TrangThai != TrangThaiDon.DaHuy)
                    throw new InvalidOperationException("Chỉ có thể xóa đơn đang chờ duyệt hoặc đã hủy");
            }
            else
            {
                // Giám Đốc/Trưởng Phòng: Có thể xóa DangChoDuyet và DaHuy
                // ĐÃ CHECK: Không xóa DaChapThuan và BiTuChoi ở trên
            }

            return await _donYeuCauRepo.DeleteAsync(donId);
        }

        #endregion

        #region Business Operations

        public async Task<PagedResult<DonYeuCauDto>> GetMyDonsAsync(
            Guid nhanVienId,
            int pageNumber,
            int pageSize,
            string? maDon = null,
            string? lyDo = null,
            LoaiDonYeuCau? loaiDon = null,
            TrangThaiDon? trangThai = null)
        {
            var (items, totalCount) = await _donYeuCauRepo.GetByNhanVienIdAsync(
                nhanVienId, pageNumber, pageSize, maDon, lyDo, loaiDon, trangThai);

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

            // Không cho phép tự duyệt đơn của chính mình
            var don = await _donYeuCauRepo.GetByIdAsync(donId);
            if (don == null)
                throw new InvalidOperationException("Không tìm thấy đơn yêu cầu");
            
            if (don.NhanVienId == nguoiDuyetId)
                throw new InvalidOperationException("Không thể tự duyệt đơn của chính mình");

            var approvedDon = await _donYeuCauRepo.DuyetDonAsync(
                donId, nguoiDuyetId, TrangThaiDon.DaChapThuan, ghiChu);

            // Lưu IDs để dùng trong background task
            var donIdCopy = approvedDon.Id;
            var nguoiDuyetIdCopy = nguoiDuyet.Id;
            var serviceScopeFactory = _serviceScopeFactory;

            // Cập nhật Telegram message (fire-and-forget với scope mới)
            _ = Task.Run(async () => 
            {
                try
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var nhanVienRepo = scope.ServiceProvider.GetRequiredService<INhanVienRepository>();
                    var donYeuCauRepo = scope.ServiceProvider.GetRequiredService<IDonYeuCauRepository>();
                    var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DonYeuCauService>>();

                    // Lấy lại data từ DB
                    var don = await donYeuCauRepo.GetByIdAsync(donIdCopy);
                    var nguoiDuyetData = await nhanVienRepo.GetByIdAsync(nguoiDuyetIdCopy);

                    if (don != null && nguoiDuyetData != null)
                    {
                        await telegramService.CapNhatTrangThaiDonAsync(don, nguoiDuyetData);
                    }
                }
                catch (Exception ex)
                {
                    var scopedLogger = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<DonYeuCauService>>();
                    scopedLogger.LogError(ex, $"❌ Lỗi cập nhật Telegram: {ex.Message}");
                }
            });

            return _mapper.Map<DonYeuCauDto>(approvedDon);
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

            // Không cho phép tự duyệt đơn của chính mình
            var donToReject = await _donYeuCauRepo.GetByIdAsync(donId);
            if (donToReject == null)
                throw new InvalidOperationException("Không tìm thấy đơn yêu cầu");
            
            if (donToReject.NhanVienId == nguoiDuyetId)
                throw new InvalidOperationException("Không thể tự duyệt đơn của chính mình");

            var rejectedDon = await _donYeuCauRepo.DuyetDonAsync(
                donId, nguoiDuyetId, TrangThaiDon.BiTuChoi, ghiChu);

            // Cập nhật Telegram message (fire-and-forget)
            var serviceScopeFactory = _serviceScopeFactory;
            var donIdCopy = rejectedDon.Id;
            var nguoiDuyetIdCopy = nguoiDuyet.Id;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var nhanVienRepo = scope.ServiceProvider.GetRequiredService<INhanVienRepository>();
                    var donYeuCauRepo = scope.ServiceProvider.GetRequiredService<IDonYeuCauRepository>();
                    var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DonYeuCauService>>();
                    
                    // Lấy lại data từ DB
                    var don = await donYeuCauRepo.GetByIdAsync(donIdCopy);
                    var nguoiDuyetData = await nhanVienRepo.GetByIdAsync(nguoiDuyetIdCopy);
                    
                    if (don == null || nguoiDuyetData == null)
                        return;
                    
                    await telegramService.CapNhatTrangThaiDonAsync(don, nguoiDuyetData);
                }
                catch (Exception ex)
                {
                    var scopedLogger = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<DonYeuCauService>>();
                    scopedLogger.LogError(ex, $"❌ Lỗi cập nhật Telegram: {ex.Message}");
                }
            });

            return _mapper.Map<DonYeuCauDto>(rejectedDon);
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

        public async Task<List<NgayNghiInfoDto>> GetNgayDaNghiAsync(
            Guid nhanVienId, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            // Dictionary để gộp thông tin nhiều đơn cùng ngày
            // Key: yyyy-MM-dd, Value: NgayNghiInfoDto
            var ngayNghiDict = new Dictionary<string, NgayNghiInfoDto>();
            var pageNumber = 1;
            const int pageSize = 100;
            
            while (true)
            {
                // Lấy đơn nghỉ phép đã được chấp thuận theo từng batch
                var filter = new FilterDonYeuCauDto
                {
                    NhanVienId = nhanVienId,
                    LoaiDon = LoaiDonYeuCau.NghiPhep,
                    TrangThai = TrangThaiDon.DaChapThuan,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var (dons, totalCount) = await _donYeuCauRepo.GetAllAsync(filter);
                
                if (!dons.Any())
                    break;

                // Xử lý từng đơn trong batch hiện tại
                foreach (var don in dons)
                {
                    if (don.NgayBatDau.HasValue && don.NgayKetThuc.HasValue && don.LoaiNghiPhep.HasValue)
                    {
                        var current = don.NgayBatDau.Value.Date;
                        var end = don.NgayKetThuc.Value.Date;
                        var loaiNghiPhep = don.LoaiNghiPhep.Value;

                        while (current <= end)
                        {
                            // Chỉ xử lý ngày nằm trong range filter
                            if ((!fromDate.HasValue || current >= fromDate.Value.Date) &&
                                (!toDate.HasValue || current <= toDate.Value.Date))
                            {
                                var dateKey = current.ToString("yyyy-MM-dd");
                                
                                // Nếu ngày này chưa có trong dict, tạo mới
                                if (!ngayNghiDict.ContainsKey(dateKey))
                                {
                                    ngayNghiDict[dateKey] = new NgayNghiInfoDto
                                    {
                                        Ngay = current,
                                        LoaiNghiPhep = loaiNghiPhep,
                                        BuoiSang = false,
                                        BuoiChieu = false,
                                        NghiCaNgay = false
                                    };
                                }
                                
                                var info = ngayNghiDict[dateKey];
                                
                                // Cập nhật thông tin dựa trên loại nghỉ
                                if (loaiNghiPhep == LoaiNghiPhep.BuoiSang)
                                {
                                    info.BuoiSang = true;
                                }
                                else if (loaiNghiPhep == LoaiNghiPhep.BuoiChieu)
                                {
                                    info.BuoiChieu = true;
                                }
                                else if (loaiNghiPhep == LoaiNghiPhep.MotNgay || loaiNghiPhep == LoaiNghiPhep.NhieuNgay)
                                {
                                    info.NghiCaNgay = true;
                                    info.BuoiSang = true;
                                    info.BuoiChieu = true;
                                }
                                
                                // Nếu cả 2 buổi đều nghỉ thì = nghỉ cả ngày
                                if (info.BuoiSang && info.BuoiChieu)
                                {
                                    info.NghiCaNgay = true;
                                }
                            }
                            current = current.AddDays(1);
                        }
                    }
                }

                if (pageNumber * pageSize >= totalCount)
                    break;

                pageNumber++;
            }

            return ngayNghiDict.Values.OrderBy(d => d.Ngay).ToList();
        }

        #endregion

        #region Private Validation Methods

        /// <summary>
        /// Sinh mã đơn tự động theo format: {LoaiDon}-{Năm}-{STT}
        /// Ví dụ: NP-2025-001, LTG-2025-002, DM-2025-003, CT-2025-004
        /// </summary>
        private async Task<string> GenerateMaDonAsync(LoaiDonYeuCau loaiDon)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = GetMaDonPrefix(loaiDon);
            
            // Đếm số đơn cùng loại và cùng năm
            var existingDonsCount = await _donYeuCauRepo.CountByLoaiAndYearAsync(loaiDon, year);
            
            // STT bắt đầu từ 1
            var stt = existingDonsCount + 1;
            
            // Format: PREFIX-YEAR-STT (STT có 3 chữ số)
            return $"{prefix}-{year}-{stt:D3}";
        }

        /// <summary>
        /// Lấy prefix cho mã đơn dựa trên loại đơn
        /// </summary>
        private string GetMaDonPrefix(LoaiDonYeuCau loaiDon) => loaiDon switch
        {
            LoaiDonYeuCau.NghiPhep => "NP",      // Nghỉ Phép
            LoaiDonYeuCau.LamThemGio => "LTG",   // Làm Thêm Giờ
            LoaiDonYeuCau.DiMuon => "DM",        // Đi Muộn
            LoaiDonYeuCau.CongTac => "CT",       // Công Tác
            _ => "DON"                             // Fallback
        };

        private async Task ValidateCreateDonAsync(Guid nhanVienId, CreateDonYeuCauDto dto)
        {
            switch (dto.LoaiDon)
            {
                case LoaiDonYeuCau.NghiPhep:
                    ValidateNghiPhep(dto);
                    // Kiểm tra xung đột nghỉ phép với logic LoaiNghiPhep
                    if (dto.NgayBatDau.HasValue && dto.NgayKetThuc.HasValue && dto.LoaiNghiPhep.HasValue)
                    {
                        var coXungDot = await _donYeuCauRepo.KiemTraXungDotNghiPhepAsync(
                            nhanVienId, 
                            dto.NgayBatDau.Value, 
                            dto.NgayKetThuc.Value, 
                            dto.LoaiNghiPhep.Value);
                        if (coXungDot)
                            throw new InvalidOperationException("Đã có đơn nghỉ phép xung đột trong khoảng thời gian này. Vui lòng kiểm tra lại.");
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
                    // Kiểm tra xung đột nghỉ phép với logic LoaiNghiPhep
                    if (dto.NgayBatDau.HasValue && dto.NgayKetThuc.HasValue && dto.LoaiNghiPhep.HasValue)
                    {
                        var coXungDot = await _donYeuCauRepo.KiemTraXungDotNghiPhepAsync(
                            nhanVienId, 
                            dto.NgayBatDau.Value, 
                            dto.NgayKetThuc.Value, 
                            dto.LoaiNghiPhep.Value,
                            excludeDonId);
                        if (coXungDot)
                            throw new InvalidOperationException("Đã có đơn nghỉ phép xung đột trong khoảng thời gian này. Vui lòng kiểm tra lại.");
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
            // Kiểm tra LoaiNghiPhep bắt buộc
            if (!dto.LoaiNghiPhep.HasValue)
                throw new InvalidOperationException("Loại nghỉ phép là bắt buộc");

            if (!dto.NgayBatDau.HasValue)
                throw new InvalidOperationException("Ngày bắt đầu là bắt buộc cho đơn nghỉ phép");
            if (!dto.NgayKetThuc.HasValue)
                throw new InvalidOperationException("Ngày kết thúc là bắt buộc cho đơn nghỉ phép");
            if (dto.NgayBatDau.Value > dto.NgayKetThuc.Value)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");
            if (dto.NgayBatDau.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Không thể tạo đơn nghỉ phép cho ngày trong quá khứ");

            // Validate logic theo LoaiNghiPhep
            var soNgay = (dto.NgayKetThuc.Value.Date - dto.NgayBatDau.Value.Date).Days;
            switch (dto.LoaiNghiPhep.Value)
            {
                case LoaiNghiPhep.BuoiSang:
                case LoaiNghiPhep.BuoiChieu:
                    if (soNgay != 0)
                        throw new InvalidOperationException("Nghỉ buổi sáng/chiều chỉ áp dụng cho cùng 1 ngày (Ngày bắt đầu = Ngày kết thúc)");
                    break;

                case LoaiNghiPhep.MotNgay:
                    if (soNgay != 0)
                        throw new InvalidOperationException("Nghỉ một ngày chỉ áp dụng cho cùng 1 ngày (Ngày bắt đầu = Ngày kết thúc)");
                    break;

                case LoaiNghiPhep.NhieuNgay:
                    if (soNgay < 1)
                        throw new InvalidOperationException("Nghỉ nhiều ngày phải từ 2 ngày trở lên");
                    break;
            }
        }

        private void ValidateNghiPhep(UpdateDonYeuCauDto dto)
        {
            // Kiểm tra LoaiNghiPhep bắt buộc
            if (!dto.LoaiNghiPhep.HasValue)
                throw new InvalidOperationException("Loại nghỉ phép là bắt buộc");

            if (!dto.NgayBatDau.HasValue)
                throw new InvalidOperationException("Ngày bắt đầu là bắt buộc cho đơn nghỉ phép");
            if (!dto.NgayKetThuc.HasValue)
                throw new InvalidOperationException("Ngày kết thúc là bắt buộc cho đơn nghỉ phép");
            if (dto.NgayBatDau.Value > dto.NgayKetThuc.Value)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");

            // Validate logic theo LoaiNghiPhep
            var soNgay = (dto.NgayKetThuc.Value.Date - dto.NgayBatDau.Value.Date).Days;
            switch (dto.LoaiNghiPhep.Value)
            {
                case LoaiNghiPhep.BuoiSang:
                case LoaiNghiPhep.BuoiChieu:
                    if (soNgay != 0)
                        throw new InvalidOperationException("Nghỉ buổi sáng/chiều chỉ áp dụng cho cùng 1 ngày (Ngày bắt đầu = Ngày kết thúc)");
                    break;

                case LoaiNghiPhep.MotNgay:
                    if (soNgay != 0)
                        throw new InvalidOperationException("Nghỉ một ngày chỉ áp dụng cho cùng 1 ngày (Ngày bắt đầu = Ngày kết thúc)");
                    break;

                case LoaiNghiPhep.NhieuNgay:
                    if (soNgay < 1)
                        throw new InvalidOperationException("Nghỉ nhiều ngày phải từ 2 ngày trở lên");
                    break;
            }
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

        #region Telegram Helper Methods

        /// <summary>
        /// Gửi thông báo Telegram trong background task với scope riêng
        /// </summary>
        private static async Task GuiThongBaoTelegramWithScopeAsync(
            DonYeuCau don, 
            NhanVien nguoiGui,
            ITelegramService telegramService,
            IDonYeuCauRepository donYeuCauRepo,
            ILogger<DonYeuCauService> logger)
        {
            try
            {
                logger.LogInformation("📲 [DON] Bắt đầu gửi thông báo Telegram cho đơn ID: {DonId}", don.Id);
                
                var messageIds = await telegramService.GuiThongBaoDonXinNghiAsync(don, nguoiGui);

                if (messageIds.Any())
                {
                    don.DaGuiTelegram = true;
                    don.ThoiGianGuiTelegram = DateTime.UtcNow;
                    don.TelegramMessageIds = JsonSerializer.Serialize(messageIds);
                    await donYeuCauRepo.UpdateAsync(don);
                    
                    logger.LogInformation("✅ [DON] Đã gửi thông báo Telegram thành công cho đơn ID: {DonId}, Số message: {Count}", 
                        don.Id, messageIds.Count);
                }
                else
                {
                    logger.LogWarning("⚠️ [DON] Không gửi được Telegram cho đơn ID: {DonId} - Không có người nhận", don.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [DON] Lỗi gửi thông báo Telegram cho đơn ID: {DonId}", don.Id);
                // Không throw để không ảnh hưởng tới việc tạo đơn
            }
        }
        #endregion
    }
}

