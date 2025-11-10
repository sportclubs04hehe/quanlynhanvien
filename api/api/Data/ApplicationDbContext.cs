using api.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
            
        }

        public DbSet<NhanVien> NhanViens => Set<NhanVien>();
        public DbSet<PhongBan> PhongBans => Set<PhongBan>();
        public DbSet<ChucVu> ChucVus => Set<ChucVu>();
        public DbSet<DonYeuCau> DonYeuCaus => Set<DonYeuCau>();
        public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<TelegramConfig> TelegramConfigs => Set<TelegramConfig>();
        public DbSet<TelegramLinkToken> TelegramLinkTokens => Set<TelegramLinkToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<NhanVien>()
                .HasOne(e => e.QuanLy)
                .WithMany()
                .HasForeignKey(e => e.QuanLyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DonYeuCau>()
                .HasOne(d => d.NhanVien)
                .WithMany(n => n.DonYeuCaus)
                .HasForeignKey(d => d.NhanVienId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DonYeuCau>()
                .HasOne(d => d.NguoiDuyet)
                .WithMany()
                .HasForeignKey(d => d.DuocChapThuanBoi)
                .OnDelete(DeleteBehavior.Restrict);

            // ThongBao configuration
            builder.Entity<ThongBao>()
                .HasOne(tb => tb.NhanVien)
                .WithMany()
                .HasForeignKey(tb => tb.NhanVienId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ThongBao>()
                .HasOne(tb => tb.DonYeuCau)
                .WithMany(d => d.ThongBaos)
                .HasForeignKey(tb => tb.DonYeuCauId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa đơn → Xóa thông báo liên quan

            // RefreshToken configuration
            builder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            builder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure DateTime columns to use timestamptz (timestamp with timezone)
            builder.Entity<RefreshToken>()
                .Property(rt => rt.CreatedAt)
                .HasColumnType("timestamptz");

            builder.Entity<RefreshToken>()
                .Property(rt => rt.ExpiresAt)
                .HasColumnType("timestamptz");

            builder.Entity<RefreshToken>()
                .Property(rt => rt.RevokedAt)
                .HasColumnType("timestamptz");

            // TelegramLinkToken configuration
            builder.Entity<TelegramLinkToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            builder.Entity<TelegramLinkToken>()
                .HasOne(t => t.NhanVien)
                .WithMany()
                .HasForeignKey(t => t.NhanVienId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TelegramLinkToken>()
                .Property(t => t.CreatedAt)
                .HasColumnType("timestamptz");

            builder.Entity<TelegramLinkToken>()
                .Property(t => t.ExpiresAt)
                .HasColumnType("timestamptz");

            builder.Entity<TelegramLinkToken>()
                .Property(t => t.UsedAt)
                .HasColumnType("timestamptz");
        }
    }
}
