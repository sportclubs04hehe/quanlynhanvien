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
        public DbSet<DonXinNghiPhep> DonXinNghiPheps => Set<DonXinNghiPhep>();
        public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<NhanVien>()
                .HasOne(e => e.QuanLy)
                .WithMany()
                .HasForeignKey(e => e.QuanLyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DonXinNghiPhep>()
                .HasOne(d => d.NhanVien)
                .WithMany(n => n.DonXinNghiPhep)
                .HasForeignKey(d => d.NhanVienId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DonXinNghiPhep>()
                .HasOne(d => d.NguoiDuyet)
                .WithMany()
                .HasForeignKey(d => d.DuocChapThuanBoi)
                .OnDelete(DeleteBehavior.Restrict);

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
        }
    }
}
