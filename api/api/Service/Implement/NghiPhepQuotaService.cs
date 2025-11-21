using api.DTO;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace api.Service.Implement
{
    public class NghiPhepQuotaService : INghiPhepQuotaService
    {
        private readonly INghiPhepQuotaRepository _quotaRepo;
        private readonly IDonYeuCauRepository _donRepo;
        private readonly IMapper _mapper;

        public NghiPhepQuotaService(
            INghiPhepQuotaRepository quotaRepo,
            IDonYeuCauRepository donRepo,
            IMapper mapper)
        {
            _quotaRepo = quotaRepo;
            _donRepo = donRepo;
            _mapper = mapper;
        }

        public async Task<NghiPhepQuotaDto> GetOrCreateQuotaAsync(Guid nhanVienId, int nam, int thang)
        {
            var quota = await _quotaRepo.GetOrCreateQuotaAsync(nhanVienId, nam, thang);
            
            // Recalculate để đảm bảo dữ liệu chính xác
            await _quotaRepo.RecalculateQuotaAsync(nhanVienId, nam, thang);
            
            // Reload quota sau khi recalculate
            quota = await _quotaRepo.GetByNhanVienAndMonthAsync(nhanVienId, nam, thang);
            
            return _mapper.Map<NghiPhepQuotaDto>(quota);
        }

        public async Task<LichNghiDashboardDto> GetLichNghiDashboardAsync(Guid nhanVienId, int? nam = null, int? thang = null)
        {
            var now = DateTime.UtcNow;
            var targetNam = nam ?? now.Year;
            var targetThang = thang ?? now.Month;

            var dashboard = new LichNghiDashboardDto();

            // 1. Quota tháng hiện tại
            dashboard.QuotaThangHienTai = await GetOrCreateQuotaAsync(nhanVienId, targetNam, targetThang);

            // 2. Tổng ngày nghỉ trong năm
            var quotasNam = await _quotaRepo.GetByNhanVienAndYearAsync(nhanVienId, targetNam);
            dashboard.TongNgayNghiTrongNam = quotasNam.Sum(q => q.SoNgayDaSuDung);
            dashboard.TongGioLamThemTrongNam = quotasNam.Sum(q => q.TongSoGioLamThem);

            // 3. Calendar view tháng hiện tại
            dashboard.CalendarThangHienTai = await GetCalendarAsync(nhanVienId, targetNam, targetThang);

            // 4. Đơn nghỉ sắp tới (approved, chưa đến ngày)
            var donSapToi = await _donRepo.GetUpcomingDonsAsync(nhanVienId);
            dashboard.DonNghiSapToi = _mapper.Map<List<DonYeuCauDto>>(donSapToi);

            // 5. Cảnh báo
            if (dashboard.QuotaThangHienTai.DaVuotQuota)
            {
                var soNgayVuot = Math.Abs(dashboard.QuotaThangHienTai.SoNgayPhepConLai);
                dashboard.CanhBao.Add($"⚠️ Bạn đã nghỉ {dashboard.QuotaThangHienTai.SoNgayDaSuDung} ngày, vượt hạn mức {dashboard.QuotaThangHienTai.SoNgayPhepThang} ngày (dư {soNgayVuot} ngày)!");
            }

            if (dashboard.TongNgayNghiTrongNam > 12)
            {
                var soNgayVuotNam = dashboard.TongNgayNghiTrongNam - 12;
                dashboard.CanhBao.Add($"⚠️ Bạn đã nghỉ {dashboard.TongNgayNghiTrongNam} ngày trong năm {targetNam}, vượt hạn mức 12 ngày (dư {soNgayVuotNam} ngày)!");
            }

            return dashboard;
        }

        public async Task<LichNghiCalendarDto> GetCalendarAsync(Guid nhanVienId, int nam, int thang)
        {
            var startDate = DateTime.SpecifyKind(new DateTime(nam, thang, 1), DateTimeKind.Utc);
            var endDate = DateTime.SpecifyKind(startDate.AddMonths(1).AddDays(-1), DateTimeKind.Utc);

            var calendar = new LichNghiCalendarDto
            {
                Nam = nam,
                Thang = thang
            };

            // Lấy tất cả đơn nghỉ phép đã approved trong tháng (CHỈ NghiPhep, không bao gồm CongTac)
            var dons = await _donRepo.GetDonsByDateRangeAsync(
                nhanVienId, 
                startDate, 
                endDate, 
                TrangThaiDon.DaChapThuan);

            // Filter chỉ lấy nghỉ phép
            dons = dons.Where(d => d.LoaiDon == LoaiDonYeuCau.NghiPhep).ToList();

            // Convert sang NgayNghiDetailDto
            foreach (var don in dons)
            {
                if (!don.NgayBatDau.HasValue || !don.NgayKetThuc.HasValue)
                    continue;

                var ngayBatDau = don.NgayBatDau.Value.Date;
                var ngayKetThuc = don.NgayKetThuc.Value.Date;

                // Nếu đơn nhiều ngày, tạo entry cho mỗi ngày
                if (don.LoaiNghiPhep == LoaiNghiPhep.NhieuNgay)
                {
                    for (var date = ngayBatDau; date <= ngayKetThuc; date = date.AddDays(1))
                    {
                        if (date.Month == thang && date.Year == nam)
                        {
                            calendar.NgayDaNghi.Add(new NgayNghiDetailDto
                            {
                                Ngay = date,
                                DonYeuCauId = don.Id,
                                MaDon = don.MaDon ?? "",
                                LoaiDon = don.LoaiDon.ToDisplayName(),
                                LoaiNghiPhep = don.LoaiNghiPhep?.ToDisplayName(),
                                SoNgay = 1,
                                LyDo = don.LyDo
                            });
                        }
                    }
                }
                else
                {
                    // Nghỉ 1 ngày hoặc nửa ngày
                    decimal soNgay = don.LoaiNghiPhep switch
                    {
                        LoaiNghiPhep.BuoiSang => 0.5m,
                        LoaiNghiPhep.BuoiChieu => 0.5m,
                        LoaiNghiPhep.MotNgay => 1m,
                        _ => 1m
                    };

                    calendar.NgayDaNghi.Add(new NgayNghiDetailDto
                    {
                        Ngay = ngayBatDau,
                        DonYeuCauId = don.Id,
                        MaDon = don.MaDon ?? "",
                        LoaiDon = don.LoaiDon.ToDisplayName(),
                        LoaiNghiPhep = don.LoaiNghiPhep?.ToDisplayName(),
                        SoNgay = soNgay,
                        LyDo = don.LyDo
                    });
                }
            }

            // Tính tổng số ngày nghỉ và giờ làm thêm trong tháng
            calendar.SoNgayNghiTrongThang = calendar.NgayDaNghi.Sum(n => n.SoNgay);

            // Lấy riêng đơn làm thêm vì dùng NgayLamThem thay vì NgayBatDau/NgayKetThuc
            var donLamThemTrongThang = await _donRepo.GetDonsByLoaiDonAsync(
                nhanVienId,
                LoaiDonYeuCau.LamThemGio,
                TrangThaiDon.DaChapThuan,
                nam,
                thang);
            
            calendar.SoGioLamThemTrongThang = donLamThemTrongThang.Sum(d => d.SoGioLamThem ?? 0);

            return calendar;
        }

        public async Task<NghiPhepQuotaDto> UpdateQuotaAsync(Guid quotaId, UpsertNghiPhepQuotaDto dto)
        {
            var quota = await _quotaRepo.GetByNhanVienAndMonthAsync(dto.NhanVienId, dto.Nam, dto.Thang);
            
            if (quota == null)
                throw new InvalidOperationException("Không tìm thấy quota");

            quota.SoNgayPhepThang = dto.SoNgayPhepThang;
            quota.GhiChu = dto.GhiChu;
            
            quota = await _quotaRepo.UpdateAsync(quota);
            
            return _mapper.Map<NghiPhepQuotaDto>(quota);
        }

        public async Task RecalculateQuotaAsync(Guid nhanVienId, int nam, int thang)
        {
            await _quotaRepo.RecalculateQuotaAsync(nhanVienId, nam, thang);
        }

        public async Task<List<NghiPhepQuotaDto>> GetQuotasByMonthAsync(int nam, int thang, Guid? phongBanId = null)
        {
            var quotas = await _quotaRepo.GetQuotasByMonthAsync(nam, thang, phongBanId);
            return _mapper.Map<List<NghiPhepQuotaDto>>(quotas);
        }

        public async Task<BulkQuotaResultDto> BulkCreateOrUpdateQuotaAsync(BulkQuotaRequestDto request)
        {
            var result = new BulkQuotaResultDto
            {
                SoLuongTaoMoi = 0,
                SoLuongCapNhat = 0,
                SoLuongBoQua = 0
            };

            try
            {
                // Lấy danh sách nhân viên
                var nhanViens = await _quotaRepo.GetNhanViensForBulkAsync(request.PhongBanId);
                result.TongSoNhanVien = nhanViens.Count;

                foreach (var nhanVien in nhanViens)
                {
                    try
                    {
                        // Kiểm tra xem quota đã tồn tại chưa
                        var existingQuota = await _quotaRepo.GetByNhanVienAndMonthAsync(nhanVien.Id, request.Nam, request.Thang);

                        if (existingQuota == null)
                        {
                            // Tạo mới
                            var newQuota = new NghiPhepQuota
                            {
                                Id = Guid.NewGuid(),
                                NhanVienId = nhanVien.Id,
                                Nam = request.Nam,
                                Thang = request.Thang,
                                SoNgayPhepThang = request.SoNgayPhepThang,
                                GhiChu = request.GhiChu,
                                NgayTao = DateTime.UtcNow
                            };
                            await _quotaRepo.CreateAsync(newQuota);
                            result.SoLuongTaoMoi++;
                        }
                        else
                        {
                            // Cập nhật
                            existingQuota.SoNgayPhepThang = request.SoNgayPhepThang;
                            existingQuota.GhiChu = request.GhiChu;
                            existingQuota.NgayCapNhat = DateTime.UtcNow;
                            await _quotaRepo.UpdateAsync(existingQuota);
                            result.SoLuongCapNhat++;
                        }

                        // Recalculate để đảm bảo số ngày đã sử dụng chính xác
                        await _quotaRepo.RecalculateQuotaAsync(nhanVien.Id, request.Nam, request.Thang);
                    }
                    catch (Exception ex)
                    {
                        result.SoLuongBoQua++;
                        result.Errors.Add($"Nhân viên {nhanVien.TenDayDu}: {ex.Message}");
                    }
                }

                var scopeText = request.PhongBanId.HasValue ? "phòng ban" : "toàn công ty";
                result.Message = $"Đã cấu hình hạn mức nghỉ phép cho {scopeText} tháng {request.Thang}/{request.Nam}. " +
                                 $"Tạo mới: {result.SoLuongTaoMoi}, Cập nhật: {result.SoLuongCapNhat}, Bỏ qua: {result.SoLuongBoQua}.";
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Lỗi hệ thống: {ex.Message}");
                result.Message = "Có lỗi xảy ra khi cấu hình hàng loạt";
            }

            return result;
        }
    }
}
