using api.Model;
using api.Model.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    /// <summary>
    /// Seed dữ liệu ban đầu cho database (Roles, Admin user, etc.)
    /// </summary>
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Khởi tạo Roles và Admin user mặc định
        /// </summary>
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Seed Roles
            await SeedRolesAsync(roleManager);

            // 2. Seed Admin User + NhanVien (tạo tài khoản Giám Đốc mặc định)
            await SeedAdminUserAsync(userManager, roleManager, dbContext);
        }

        /// <summary>
        /// Tạo 3 roles: GiamDoc, TruongPhong, NhanVien
        /// </summary>
        private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            var roles = new[]
            {
                AppRolesExtensions.GiamDoc,
                AppRolesExtensions.TruongPhong,
                AppRolesExtensions.NhanVien
            };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole<Guid>
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper()
                    };

                    var result = await roleManager.CreateAsync(role);
                    
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"✅ Role '{roleName}' created successfully");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️  Role '{roleName}' already exists");
                }
            }
        }

        /// <summary>
        /// Tạo tài khoản Admin (Giám Đốc) mặc định + NhanVien tương ứng
        /// </summary>
        private static async Task SeedAdminUserAsync(
            UserManager<User> userManager, 
            RoleManager<IdentityRole<Guid>> roleManager,
            ApplicationDbContext dbContext)
        {
            const string adminEmail = "admin@company.com";
            const string adminPassword = "Admin@123"; 

            var existingUser = await userManager.FindByEmailAsync(adminEmail);
            if (existingUser == null)
            {
                var adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumber = "0000000000"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                
                if (result.Succeeded)
                {
                    // Gán role Giám Đốc
                    await userManager.AddToRoleAsync(adminUser, AppRolesExtensions.GiamDoc);
                    
                    // ✅ TẠO NHANVIEN TƯƠNG ỨNG với cùng ID
                    var adminNhanVien = new NhanVien
                    {
                        Id = adminUser.Id, // ← QUAN TRỌNG: Cùng ID với User
                        TenDayDu = "Giám Đốc Hệ Thống",
                        NgayVaoLam = DateTime.UtcNow,
                        Status = NhanVienStatus.Active,
                        User = adminUser // Navigation property
                    };

                    dbContext.NhanViens.Add(adminNhanVien);
                    await dbContext.SaveChangesAsync();
                    
                    Console.WriteLine($"✅ Admin user '{adminEmail}' created successfully with role GiamDoc");
                    Console.WriteLine($"✅ Admin NhanVien record created with ID: {adminNhanVien.Id}");
                    Console.WriteLine($"   Password: {adminPassword} (Please change after first login!)");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine($"ℹ️  Admin user '{adminEmail}' already exists");
                
                // Đảm bảo admin có role GiamDoc
                if (!await userManager.IsInRoleAsync(existingUser, AppRolesExtensions.GiamDoc))
                {
                    await userManager.AddToRoleAsync(existingUser, AppRolesExtensions.GiamDoc);
                    Console.WriteLine($"✅ Added GiamDoc role to existing admin user");
                }

                // ✅ Kiểm tra và tạo NhanVien nếu chưa có
                var existingNhanVien = await dbContext.NhanViens.FindAsync(existingUser.Id);
                if (existingNhanVien == null)
                {
                    var adminNhanVien = new NhanVien
                    {
                        Id = existingUser.Id,
                        TenDayDu = "Giám Đốc Hệ Thống",
                        NgayVaoLam = DateTime.UtcNow,
                        Status = NhanVienStatus.Active,
                        User = existingUser
                    };

                    dbContext.NhanViens.Add(adminNhanVien);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"✅ Created missing NhanVien record for admin user");
                }
            }
        }
    }
}
