using api.Common;
using api.Data;
using api.DTO;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace api.Repository.Implement
{
    public class DonYeuCauRepository : IDonYeuCauRepository
    {
        private readonly ApplicationDbContext _context;

        public DonYeuCauRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CRUD cơ bản

        public async Task<(List<DonYeuCau> Items, int TotalCount)> GetAllAsync(FilterDonYeuCauDto filter)
        {
            var query = _context.DonYeuCaus
                .Include(d => d.NhanVien)
                    .ThenInclude(nv => nv.User)
                .Include(d => d.NhanVien.PhongBan)
                .Include(d => d.NhanVien.ChucVu)
                .Include(d => d.NguoiDuyet)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var lowerSearch = filter.SearchTerm.ToLower();
                query = query.Where(d =>
                    d.NhanVien.TenDayDu.ToLower().Contains(lowerSearch) ||
                    d.LyDo.ToLower().Contains(lowerSearch)
                );
            }

            // Filter by MaDon
            if (!string.IsNullOrWhiteSpace(filter.MaDon))
            {
                query = query.Where(d => d.MaDon != null && d.MaDon.ToLower().Contains(filter.MaDon.ToLower()));
            }

            // Filter by LoaiDon
            if (filter.LoaiDon.HasValue)
            {
                query = query.Where(d => d.LoaiDon == filter.LoaiDon.Value);
            }

            // Filter by TrangThai
            if (filter.TrangThai.HasValue)
            {
                query = query.Where(d => d.TrangThai == filter.TrangThai.Value);
            }

            // Filter by NhanVienId
            if (filter.NhanVienId.HasValue)
            {
                query = query.Where(d => d.NhanVienId == filter.NhanVienId.Value);
            }

            // Filter by NguoiDuyetId
            if (filter.NguoiDuyetId.HasValue)
            {
                query = query.Where(d => d.DuocChapThuanBoi == filter.NguoiDuyetId.Value);
            }

            // Filter by PhongBanId
            if (filter.PhongBanId.HasValue)
            {
                query = query.Where(d => d.NhanVien.PhongBanId == filter.PhongBanId.Value);
            }

            // Filter by date range
            // Frontend gửi date-only (VD: 2025-11-14), cần convert sang UTC+7 rồi sang UTC để match với database
            if (filter.TuNgay.HasValue)
            {
                var tuNgay = DateTimeHelper.ToUtcFromVietnam(filter.TuNgay.Value.Date);
                query = query.Where(d => d.NgayTao >= tuNgay);
            }

            if (filter.DenNgay.HasValue)
            {
                var denNgay = DateTimeHelper.ToUtcFromVietnam(filter.DenNgay.Value.Date.AddDays(1));
                query = query.Where(d => d.NgayTao < denNgay);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.NgayTao)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<DonYeuCau?> GetByIdAsync(Guid id)
        {
            return await _context.DonYeuCaus
                .Include(d => d.NhanVien)
                    .ThenInclude(nv => nv.User)
                .Include(d => d.NhanVien.PhongBan)
                .Include(d => d.NhanVien.ChucVu)
                .Include(d => d.NguoiDuyet)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<DonYeuCau> CreateAsync(DonYeuCau donYeuCau)
        {
            _context.DonYeuCaus.Add(donYeuCau);
            await _context.SaveChangesAsync();
            
            // Load lại navigation properties
            await _context.Entry(donYeuCau)
                .Reference(d => d.NhanVien)
                .LoadAsync();
            await _context.Entry(donYeuCau.NhanVien)
                .Reference(nv => nv.User)
                .LoadAsync();
            await _context.Entry(donYeuCau.NhanVien)
                .Reference(nv => nv.PhongBan)
                .LoadAsync();
            await _context.Entry(donYeuCau.NhanVien)
                .Reference(nv => nv.ChucVu)
                .LoadAsync();
            
            return donYeuCau;
        }

        public async Task<DonYeuCau> UpdateAsync(DonYeuCau donYeuCau)
        {
            donYeuCau.NgayCapNhat = DateTime.UtcNow;
            _context.DonYeuCaus.Update(donYeuCau);
            await _context.SaveChangesAsync();
            return donYeuCau;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var don = await _context.DonYeuCaus.FindAsync(id);
            if (don == null)
                return false;

            _context.DonYeuCaus.Remove(don);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.DonYeuCaus.AnyAsync(d => d.Id == id);
        }

        #endregion

        #region Business Logic Methods

        public async Task<(List<DonYeuCau> Items, int TotalCount)> GetByNhanVienIdAsync(
            Guid nhanVienId,
            int pageNumber,
            int pageSize,
            LoaiDonYeuCau? loaiDon = null,
            TrangThaiDon? trangThai = null)
        {
            var query = _context.DonYeuCaus
                .Include(d => d.NhanVien)
                    .ThenInclude(nv => nv.User)
                .Include(d => d.NhanVien.PhongBan)
                .Include(d => d.NhanVien.ChucVu)
                .Include(d => d.NguoiDuyet)
                .Where(d => d.NhanVienId == nhanVienId);

            if (loaiDon.HasValue)
            {
                query = query.Where(d => d.LoaiDon == loaiDon.Value);
            }

            if (trangThai.HasValue)
            {
                query = query.Where(d => d.TrangThai == trangThai.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.NgayTao)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<DonYeuCau> Items, int TotalCount)> GetDonCanDuyetAsync(
            Guid nguoiDuyetId,
            int pageNumber,
            int pageSize)
        {
            // Lấy thông tin người duyệt
            var nguoiDuyet = await _context.NhanViens
                .Include(nv => nv.PhongBan)
                .FirstOrDefaultAsync(nv => nv.Id == nguoiDuyetId);

            if (nguoiDuyet == null)
                return (new List<DonYeuCau>(), 0);

            var query = _context.DonYeuCaus
                .Include(d => d.NhanVien)
                    .ThenInclude(nv => nv.User)
                .Include(d => d.NhanVien.PhongBan)
                .Include(d => d.NhanVien.ChucVu)
                .Include(d => d.NguoiDuyet)
                .Where(d => d.TrangThai == TrangThaiDon.DangChoDuyet)
                .Where(d => d.NhanVienId != nguoiDuyetId); // ❌ Không cho tự duyệt đơn của chính mình

            // Logic: 
            // - Trưởng Phòng duyệt đơn của nhân viên trong phòng (trừ đơn của chính mình)
            // - Giám Đốc duyệt tất cả đơn (trừ đơn của chính mình)
            if (nguoiDuyet.PhongBanId.HasValue)
            {
                // Trưởng phòng - chỉ duyệt đơn trong phòng
                query = query.Where(d => d.NhanVien.PhongBanId == nguoiDuyet.PhongBanId.Value);
            }
            // Nếu không có PhongBanId thì là Giám Đốc - duyệt tất cả

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.NgayTao) // Đơn cũ nhất lên trước
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<DonYeuCau> Items, int TotalCount)> GetByPhongBanIdAsync(
            Guid phongBanId,
            int pageNumber,
            int pageSize,
            TrangThaiDon? trangThai = null)
        {
            var query = _context.DonYeuCaus
                .Include(d => d.NhanVien)
                    .ThenInclude(nv => nv.User)
                .Include(d => d.NhanVien.PhongBan)
                .Include(d => d.NhanVien.ChucVu)
                .Include(d => d.NguoiDuyet)
                .Where(d => d.NhanVien.PhongBanId == phongBanId);

            if (trangThai.HasValue)
            {
                query = query.Where(d => d.TrangThai == trangThai.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.NgayTao)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<DonYeuCau>> GetByTrangThaiAsync(TrangThaiDon trangThai)
        {
            return await _context.DonYeuCaus
                .Include(d => d.NhanVien)
                    .ThenInclude(nv => nv.User)
                .Include(d => d.NhanVien.PhongBan)
                .Include(d => d.NhanVien.ChucVu)
                .Include(d => d.NguoiDuyet)
                .Where(d => d.TrangThai == trangThai)
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();
        }

        public async Task<DonYeuCau> DuyetDonAsync(Guid donId, Guid nguoiDuyetId, TrangThaiDon trangThai, string? ghiChu)
        {
            var don = await GetByIdAsync(donId);
            if (don == null)
                throw new InvalidOperationException("Không tìm thấy đơn");

            if (don.TrangThai != TrangThaiDon.DangChoDuyet)
                throw new InvalidOperationException("Đơn không ở trạng thái chờ duyệt");

            don.TrangThai = trangThai;
            don.DuocChapThuanBoi = nguoiDuyetId;
            don.GhiChuNguoiDuyet = ghiChu;
            don.NgayDuyet = DateTime.UtcNow;
            don.NgayCapNhat = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            // Reload NguoiDuyet
            await _context.Entry(don)
                .Reference(d => d.NguoiDuyet)
                .LoadAsync();
            
            return don;
        }

        public async Task<bool> HuyDonAsync(Guid donId, Guid nhanVienId)
        {
            var don = await _context.DonYeuCaus
                .FirstOrDefaultAsync(d => d.Id == donId && d.NhanVienId == nhanVienId);

            if (don == null)
                return false;

            if (don.TrangThai != TrangThaiDon.DangChoDuyet)
                return false;

            don.TrangThai = TrangThaiDon.DaHuy;
            don.NgayCapNhat = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsOwnerAsync(Guid donId, Guid nhanVienId)
        {
            return await _context.DonYeuCaus
                .AnyAsync(d => d.Id == donId && d.NhanVienId == nhanVienId);
        }

        public async Task<bool> CanEditAsync(Guid donId)
        {
            var don = await _context.DonYeuCaus.FindAsync(donId);
            return don != null && don.TrangThai == TrangThaiDon.DangChoDuyet;
        }

        public async Task<bool> CanCancelAsync(Guid donId)
        {
            var don = await _context.DonYeuCaus.FindAsync(donId);
            return don != null && don.TrangThai == TrangThaiDon.DangChoDuyet;
        }

        #endregion

        #region Thống kê

        public async Task<ThongKeDonYeuCauDto> ThongKeByNhanVienAsync(Guid nhanVienId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.DonYeuCaus.Where(d => d.NhanVienId == nhanVienId);

            if (fromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
                query = query.Where(d => d.NgayTao >= from);
            }

            if (toDate.HasValue)
            {
                var endDate = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc).AddDays(1);
                query = query.Where(d => d.NgayTao < endDate);
            }

            var dons = await query.ToListAsync();

            return new ThongKeDonYeuCauDto
            {
                TongSoDon = dons.Count,
                DangChoDuyet = dons.Count(d => d.TrangThai == TrangThaiDon.DangChoDuyet),
                DaChapThuan = dons.Count(d => d.TrangThai == TrangThaiDon.DaChapThuan),
                BiTuChoi = dons.Count(d => d.TrangThai == TrangThaiDon.BiTuChoi),
                DaHuy = dons.Count(d => d.TrangThai == TrangThaiDon.DaHuy),
                SoDonNghiPhep = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.NghiPhep),
                SoDonLamThemGio = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.LamThemGio),
                SoDonDiMuon = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.DiMuon),
                SoDonCongTac = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.CongTac)
            };
        }

        public async Task<ThongKeDonYeuCauDto> ThongKeByPhongBanAsync(Guid phongBanId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.DonYeuCaus
                .Where(d => d.NhanVien.PhongBanId == phongBanId);

            if (fromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
                query = query.Where(d => d.NgayTao >= from);
            }

            if (toDate.HasValue)
            {
                var endDate = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc).AddDays(1);
                query = query.Where(d => d.NgayTao < endDate);
            }

            var dons = await query.ToListAsync();

            return new ThongKeDonYeuCauDto
            {
                TongSoDon = dons.Count,
                DangChoDuyet = dons.Count(d => d.TrangThai == TrangThaiDon.DangChoDuyet),
                DaChapThuan = dons.Count(d => d.TrangThai == TrangThaiDon.DaChapThuan),
                BiTuChoi = dons.Count(d => d.TrangThai == TrangThaiDon.BiTuChoi),
                DaHuy = dons.Count(d => d.TrangThai == TrangThaiDon.DaHuy),
                SoDonNghiPhep = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.NghiPhep),
                SoDonLamThemGio = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.LamThemGio),
                SoDonDiMuon = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.DiMuon),
                SoDonCongTac = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.CongTac)
            };
        }

        public async Task<ThongKeDonYeuCauDto> ThongKeToanCongTyAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.DonYeuCaus.AsQueryable();

            if (fromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
                query = query.Where(d => d.NgayTao >= from);
            }

            if (toDate.HasValue)
            {
                var endDate = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc).AddDays(1);
                query = query.Where(d => d.NgayTao < endDate);
            }

            var dons = await query.ToListAsync();

            return new ThongKeDonYeuCauDto
            {
                TongSoDon = dons.Count,
                DangChoDuyet = dons.Count(d => d.TrangThai == TrangThaiDon.DangChoDuyet),
                DaChapThuan = dons.Count(d => d.TrangThai == TrangThaiDon.DaChapThuan),
                BiTuChoi = dons.Count(d => d.TrangThai == TrangThaiDon.BiTuChoi),
                DaHuy = dons.Count(d => d.TrangThai == TrangThaiDon.DaHuy),
                SoDonNghiPhep = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.NghiPhep),
                SoDonLamThemGio = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.LamThemGio),
                SoDonDiMuon = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.DiMuon),
                SoDonCongTac = dons.Count(d => d.LoaiDon == LoaiDonYeuCau.CongTac)
            };
        }

        public async Task<int> CountDonChoDuyetAsync(Guid nguoiDuyetId)
        {
            // Lấy thông tin người duyệt
            var nguoiDuyet = await _context.NhanViens
                .Include(nv => nv.PhongBan)
                .FirstOrDefaultAsync(nv => nv.Id == nguoiDuyetId);

            if (nguoiDuyet == null)
                return 0;

            var query = _context.DonYeuCaus
                .Where(d => d.TrangThai == TrangThaiDon.DangChoDuyet)
                .Where(d => d.NhanVienId != nguoiDuyetId); // ❌ Không đếm đơn của chính mình

            // Logic tương tự GetDonCanDuyetAsync
            if (nguoiDuyet.PhongBanId.HasValue)
            {
                query = query.Where(d => d.NhanVien.PhongBanId == nguoiDuyet.PhongBanId.Value);
            }

            return await query.CountAsync();
        }

        #endregion

        #region Validation

        public async Task<int> CountByLoaiAndYearAsync(LoaiDonYeuCau loaiDon, int year)
        {
            var startOfYear = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfYear = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            return await _context.DonYeuCaus
                .Where(d => d.LoaiDon == loaiDon 
                         && d.NgayTao >= startOfYear 
                         && d.NgayTao <= endOfYear)
                .CountAsync();
        }

        public async Task<bool> KiemTraTrungNgayNghiAsync(Guid nhanVienId, DateTime ngayBatDau, DateTime ngayKetThuc, Guid? excludeDonId = null)
        {
            // Chuyển đổi sang UTC nếu chưa
            ngayBatDau = DateTime.SpecifyKind(ngayBatDau, DateTimeKind.Utc);
            ngayKetThuc = DateTime.SpecifyKind(ngayKetThuc, DateTimeKind.Utc);
            
            var query = _context.DonYeuCaus
                .Where(d => d.NhanVienId == nhanVienId
                    && d.LoaiDon == LoaiDonYeuCau.NghiPhep
                    && (d.TrangThai == TrangThaiDon.DangChoDuyet || d.TrangThai == TrangThaiDon.DaChapThuan)
                    && d.NgayBatDau.HasValue
                    && d.NgayKetThuc.HasValue);

            if (excludeDonId.HasValue)
            {
                query = query.Where(d => d.Id != excludeDonId.Value);
            }

            // Kiểm tra overlap: (Start1 <= End2) AND (End1 >= Start2)
            var exists = await query.AnyAsync(d =>
                d.NgayBatDau!.Value.Date <= ngayKetThuc.Date &&
                d.NgayKetThuc!.Value.Date >= ngayBatDau.Date
            );

            return exists;
        }

        public async Task<bool> DaCoDoiDiMuonTrongNgayAsync(Guid nhanVienId, DateTime ngay, Guid? excludeDonId = null)
        {
            // Chuyển đổi sang UTC nếu chưa
            ngay = DateTime.SpecifyKind(ngay, DateTimeKind.Utc);
            
            var query = _context.DonYeuCaus
                .Where(d => d.NhanVienId == nhanVienId
                    && d.LoaiDon == LoaiDonYeuCau.DiMuon
                    && (d.TrangThai == TrangThaiDon.DangChoDuyet || d.TrangThai == TrangThaiDon.DaChapThuan)
                    && d.NgayDiMuon.HasValue
                    && d.NgayDiMuon.Value.Date == ngay.Date);

            if (excludeDonId.HasValue)
            {
                query = query.Where(d => d.Id != excludeDonId.Value);
            }

            return await query.AnyAsync();
        }

        #endregion
    }
}

