using eSchool.Models;
using eSchool.Repositories;
using eSchool.Services;
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

                if (!dbContext.ChucVus.Any(c => c.IdChucVu == 5))
                {
                    try 
                    {
                        dbContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT ChucVus ON; INSERT INTO ChucVus (IdChucVu, TenChucVu) VALUES (5, N'System Admin'); SET IDENTITY_INSERT ChucVus OFF;");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error seeding System Admin: " + ex.Message);
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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
