using api.Data;
using api.Model;
using api.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace api.Repository.Implement
{
    /// <summary>
    /// Repository triển khai xử lý TelegramLinkToken
    /// </summary>
    public class TelegramLinkRepository : ITelegramLinkRepository
    {
        private readonly ApplicationDbContext _context;

        public TelegramLinkRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TelegramLinkToken?> GetByTokenAsync(string token, bool includeNhanVien = false)
        {
            var query = _context.TelegramLinkTokens.AsQueryable();

            if (includeNhanVien)
            {
                query = query.Include(t => t.NhanVien);
            }

            return await query.FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task<List<TelegramLinkToken>> GetUnusedTokensByNhanVienAsync(Guid nhanVienId)
        {
            return await _context.TelegramLinkTokens
                .Where(t => t.NhanVienId == nhanVienId && !t.IsUsed)
                .ToListAsync();
        }

        public async Task<TelegramLinkToken?> GetPendingTokenAsync(Guid nhanVienId)
        {
            return await _context.TelegramLinkTokens
                .Where(t => t.NhanVienId == nhanVienId && 
                           !t.IsUsed && 
                           t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TelegramLinkToken> CreateAsync(TelegramLinkToken token)
        {
            _context.TelegramLinkTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task DeleteRangeAsync(List<TelegramLinkToken> tokens)
        {
            _context.TelegramLinkTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TelegramLinkToken token)
        {
            _context.TelegramLinkTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
