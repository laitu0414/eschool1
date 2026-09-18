using eSchool.Models;
using eSchool.Repositories;
using eSchool.Services;
using eSchool.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace eschool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            QuestPDF.Settings.License = LicenseType.Community;
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(
                    new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")))
                .SetApplicationName("eSchool");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IChucVuService, ChucVuService>();
            builder.Services.AddScoped<IThongBaoService, ThongBaoService>();

            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IChucVuRepository, ChucVuRepository>();
            builder.Services.AddScoped<IThongBaoRepository, ThongBaoRepository>();

            builder.Services.AddScoped<INhatKyRepository, NhatKyRepository>();
            builder.Services.AddScoped<INhatKyService, NhatKyService>();

            builder.Services.AddScoped<IHocSinhRepository, HocSinhRepository>();
            builder.Services.AddScoped<IHocSinhService, HocSinhService>();

            builder.Services.AddScoped<IPhuHuynhRepository, PhuHuynhRepository>();
            builder.Services.AddScoped<IPhuHuynhService, PhuHuynhService>();

            builder.Services.AddScoped<IChuyenLopRepository, ChuyenLopRepository>();

            builder.Services.AddSession();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();

                var defaultRoles = new Dictionary<int, string>
                {
                    [SystemRoleIds.SystemAdmin] = "System Admin",
                    [2] = "Giáo viên",
                    [3] = "Học sinh",
                    [4] = "Phụ huynh"
                };

                foreach (var role in defaultRoles)
                {
                    if (dbContext.ChucVus.Any(c => c.IdChucVu == role.Key))
                        continue;

                    try
                    {
                        dbContext.ChucVus.Add(new ChucVu
                        {
                            IdChucVu = role.Key,
                            TenChucVu = role.Value
                        });
                        dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding role {role.Key}: {ex.Message}");
                    }
                }

                if (!dbContext.TaiKhoans.Any(t => t.IdChucVu == SystemRoleIds.SystemAdmin))
                {
                    try
                    {
                        dbContext.TaiKhoans.Add(new TaiKhoan
                        {
                            Username = "admin",
                            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                            Email = "admin@eschool.local",
                            IdChucVu = SystemRoleIds.SystemAdmin,
                            TrangThai = true,
                            BatBuocDoiMatKhau = true
                        });
                        dbContext.SaveChanges();
                        Console.WriteLine("Default admin account created: admin / 123456");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding default admin: {ex.Message}");
                    }
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            var supportedCultures = new[] { "vi-VN" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);
            app.UseRequestLocalization(localizationOptions);

            app.UseRouting();

            app.UseSession();
            app.Use(async (context, next) =>
            {
                var userId = context.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var account = await db.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(x => x.IdTaiKhoan == userId);
                    if (account == null || !account.TrangThai ||
                        account.IdChucVu != context.Session.GetInt32("RoleId"))
                    {
                        context.Session.Clear();
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                    context.Session.SetInt32("MustChangePassword", account.BatBuocDoiMatKhau ? 1 : 0);
                }
                await next();
            });

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
