using api.Data;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace api.Repository.Implement
{
    public class NghiPhepQuotaRepository : INghiPhepQuotaRepository
    {
        private readonly ApplicationDbContext _context;

        public NghiPhepQuotaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<NghiPhepQuota?> GetByNhanVienAndMonthAsync(Guid nhanVienId, int nam, int thang)
        {
            return await _context.NghiPhepQuotas
                .Include(q => q.NhanVien)
                .ThenInclude(nv => nv.PhongBan)
                .FirstOrDefaultAsync(q => q.NhanVienId == nhanVienId && q.Nam == nam && q.Thang == thang);
        }

        public async Task<List<NghiPhepQuota>> GetByNhanVienAndYearAsync(Guid nhanVienId, int nam)
        {
            return await _context.NghiPhepQuotas
                .Include(q => q.NhanVien)
                .Where(q => q.NhanVienId == nhanVienId && q.Nam == nam)
                .OrderBy(q => q.Thang)
                .ToListAsync();
        }

        public async Task<NghiPhepQuota> CreateAsync(NghiPhepQuota quota)
        {
            _context.NghiPhepQuotas.Add(quota);
            await _context.SaveChangesAsync();
            return quota;
        }

        public async Task<NghiPhepQuota> UpdateAsync(NghiPhepQuota quota)
        {
            quota.NgayCapNhat = DateTime.UtcNow;
            _context.NghiPhepQuotas.Update(quota);
            await _context.SaveChangesAsync();
            return quota;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var quota = await _context.NghiPhepQuotas.FindAsync(id);
            if (quota == null)
                return false;

            _context.NghiPhepQuotas.Remove(quota);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<NghiPhepQuota> GetOrCreateQuotaAsync(Guid nhanVienId, int nam, int thang, decimal soNgayPhepThang = 1m)
        {
            var quota = await GetByNhanVienAndMonthAsync(nhanVienId, nam, thang);

            if (quota != null)
                return quota;

            // Tạo mới nếu chưa có
            quota = new NghiPhepQuota
            {
                NhanVienId = nhanVienId,
                Nam = nam,
                Thang = thang,
                SoNgayPhepThang = soNgayPhepThang,
                SoNgayDaSuDung = 0,
                TongSoGioLamThem = 0,
                NgayTao = DateTime.UtcNow
            };

            return await CreateAsync(quota);
        }

        public async Task RecalculateQuotaAsync(Guid nhanVienId, int nam, int thang)
        {
            var quota = await GetByNhanVienAndMonthAsync(nhanVienId, nam, thang);
            if (quota == null)
                return;

            // Tính tổng ngày nghỉ phép đã approved trong tháng
            var startDate = DateTime.SpecifyKind(new DateTime(nam, thang, 1), DateTimeKind.Utc);
            var endDate = DateTime.SpecifyKind(startDate.AddMonths(1).AddDays(-1), DateTimeKind.Utc);

            var donNghiPheps = await _context.DonYeuCaus
                .Where(d => d.NhanVienId == nhanVienId
                    && d.LoaiDon == LoaiDonYeuCau.NghiPhep
                    && d.TrangThai == TrangThaiDon.DaChapThuan
                    && d.NgayBatDau.HasValue
                    && d.NgayKetThuc.HasValue
                    && d.NgayBatDau.Value.Date <= endDate
                    && d.NgayKetThuc.Value.Date >= startDate)
                .ToListAsync();

            // Tính tổng số ngày nghỉ
            decimal tongNgayNghi = 0;
            foreach (var don in donNghiPheps)
            {
                if (don.LoaiNghiPhep.HasValue)
                {
                    tongNgayNghi += don.LoaiNghiPhep.Value switch
                    {
                        LoaiNghiPhep.BuoiSang => 0.5m,
                        LoaiNghiPhep.BuoiChieu => 0.5m,
                        LoaiNghiPhep.MotNgay => 1m,
                        LoaiNghiPhep.NhieuNgay => CalculateDaysInMonthForLeave(don.NgayBatDau!.Value, don.NgayKetThuc!.Value, startDate, endDate),
                        _ => 0
                    };
                }
            }

            // Tính tổng giờ làm thêm đã approved trong tháng
            var donLamThem = await _context.DonYeuCaus
                .Where(d => d.NhanVienId == nhanVienId
                    && d.LoaiDon == LoaiDonYeuCau.LamThemGio
                    && d.TrangThai == TrangThaiDon.DaChapThuan
                    && d.NgayLamThem.HasValue
                    && d.NgayLamThem.Value.Year == nam
                    && d.NgayLamThem.Value.Month == thang
                    && d.SoGioLamThem.HasValue)
                .ToListAsync();

            decimal tongGioLamThem = donLamThem.Sum(d => d.SoGioLamThem ?? 0);

            // Cập nhật quota
            quota.SoNgayDaSuDung = tongNgayNghi;
            quota.TongSoGioLamThem = tongGioLamThem;
            await UpdateAsync(quota);
        }

        public async Task<List<NghiPhepQuota>> GetQuotasByMonthAsync(int nam, int thang, Guid? phongBanId = null)
        {
            var query = _context.NghiPhepQuotas
                .Include(q => q.NhanVien)
                .ThenInclude(nv => nv.PhongBan)
                .Where(q => q.Nam == nam && q.Thang == thang);

            if (phongBanId.HasValue)
            {
                query = query.Where(q => q.NhanVien.PhongBanId == phongBanId.Value);
            }

            return await query
                .OrderBy(q => q.NhanVien.TenDayDu)
                .ToListAsync();
        }

        public async Task<List<NhanVien>> GetNhanViensForBulkAsync(Guid? phongBanId = null)
        {
            var query = _context.NhanViens.AsQueryable();

            // Filter by phong ban if specified
            if (phongBanId.HasValue)
            {
                query = query.Where(nv => nv.PhongBanId == phongBanId.Value);
            }

            return await query
                .OrderBy(nv => nv.TenDayDu)
                .ToListAsync();
        }

        /// <summary>
        /// Tính số ngày trong khoảng thời gian nằm trong tháng (dành cho đơn NhieuNgay)
        /// VD: Nghỉ từ 18/11 - 21/11 trong tháng 11 = 4 ngày
        /// </summary>
        private static decimal CalculateDaysInMonthForLeave(DateTime ngayBatDau, DateTime ngayKetThuc, DateTime startOfMonth, DateTime endOfMonth)
        {
            // Chỉ lấy phần Date để tránh vấn đề timezone
            var start = ngayBatDau.Date > startOfMonth.Date ? ngayBatDau.Date : startOfMonth.Date;
            var end = ngayKetThuc.Date < endOfMonth.Date ? ngayKetThuc.Date : endOfMonth.Date;

            // Công thức: (end - start).Days + 1
            // VD: 21/11 - 18/11 = 3 days, +1 = 4 days
            return (decimal)(end - start).Days + 1;
        }
    }
}
