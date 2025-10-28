using api.Data;
using api.Model;
using api.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace api.Repository.Implement
{
    public class PhongBanRepository : IPhongBanRepository
    {
        private readonly ApplicationDbContext _context;

        public PhongBanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<PhongBan> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var query = _context.PhongBans
                .Include(p => p.NhanViens)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.TenPhongBan.Contains(searchTerm) || 
                                        (p.MoTa != null && p.MoTa.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.TenPhongBan)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<PhongBan?> GetByIdAsync(Guid id)
        {
            return await _context.PhongBans
                .Include(p => p.NhanViens)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PhongBan> CreateAsync(PhongBan phongBan)
        {
            _context.PhongBans.Add(phongBan);
            await _context.SaveChangesAsync();
            return phongBan;
        }

        public async Task<PhongBan> UpdateAsync(PhongBan phongBan)
        {
            _context.PhongBans.Update(phongBan);
            await _context.SaveChangesAsync();
            return phongBan;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var phongBan = await _context.PhongBans.FindAsync(id);
            if (phongBan == null)
                return false;

            _context.PhongBans.Remove(phongBan);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.PhongBans.AnyAsync(p => p.Id == id);
        }
    }
}
