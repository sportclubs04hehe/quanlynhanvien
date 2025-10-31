using api.Data;
using api.Model;
using api.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace api.Repository.Implement
{
    public class NhanVienRepository : INhanVienRepository
    {
        private readonly ApplicationDbContext _context;

        public NhanVienRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<NhanVien> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var query = _context.NhanViens
                .Include(nv => nv.User)
                .Include(nv => nv.PhongBan)
                .Include(nv => nv.ChucVu)
                .Include(nv => nv.QuanLy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(nv =>
                    nv.TenDayDu.ToLower().Contains(lowerSearch) ||
                    (nv.User.Email != null && nv.User.Email.ToLower().Contains(lowerSearch))
                );
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(nv => nv.TenDayDu)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<NhanVien?> GetByIdAsync(Guid id)
        {
            return await _context.NhanViens
                .Include(nv => nv.User)
                .Include(nv => nv.PhongBan)
                .Include(nv => nv.ChucVu)
                .Include(nv => nv.QuanLy)
                .FirstOrDefaultAsync(nv => nv.Id == id);
        }

        public async Task<NhanVien> CreateAsync(NhanVien nhanVien)
        {
            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();
            return nhanVien;
        }

        public async Task<NhanVien> UpdateAsync(NhanVien nhanVien)
        {
            _context.NhanViens.Update(nhanVien);
            await _context.SaveChangesAsync();
            return nhanVien;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null)
                return false;

            _context.NhanViens.Remove(nhanVien);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.NhanViens.AnyAsync(nv => nv.Id == id);
        }
    }
}
