using api.Data;
using api.Model;
using api.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace api.Repository.Implement
{
    public class ChucVuRepository : IChucVuRepository
    {
        private readonly ApplicationDbContext _context;

        public ChucVuRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<ChucVu> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var query = _context.ChucVus
                .Include(cv => cv.NhanViens)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(cv => cv.TenChucVu.ToLower().Contains(lowerSearch));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(cv => cv.Level)
                .ThenBy(cv => cv.TenChucVu)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<ChucVu?> GetByIdAsync(Guid id)
        {
            return await _context.ChucVus
                .Include(cv => cv.NhanViens)
                .FirstOrDefaultAsync(cv => cv.Id == id);
        }

        public async Task<ChucVu> CreateAsync(ChucVu chucVu)
        {
            _context.ChucVus.Add(chucVu);
            await _context.SaveChangesAsync();
            return chucVu;
        }

        public async Task<ChucVu> UpdateAsync(ChucVu chucVu)
        {
            _context.ChucVus.Update(chucVu);
            await _context.SaveChangesAsync();
            return chucVu;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var chucVu = await _context.ChucVus.FindAsync(id);
            if (chucVu == null)
                return false;

            _context.ChucVus.Remove(chucVu);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.ChucVus.AnyAsync(cv => cv.Id == id);
        }
    }
}
