using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Areas.Admin.Repository;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using shopping_tutorial.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ⚠️ Fix lỗi SameSite cookie khi chạy trên Windows
        AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.EnableWindows81CookieSameSite", true);

        // Connect to SQL Server
        builder.Services.AddDbContext<DataContext>(options =>
        {
            options.UseSqlServer(builder.Configuration["ConnectionStrings:ConnectDb"]);
        });

        // Email sender service
        builder.Services.AddTransient<IEmailSender, EmailSender>();

        // Add MVC
        builder.Services.AddControllersWithViews();

        // Session config
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(15);
            options.Cookie.IsEssential = true;
        });

        // Identity
        builder.Services.AddIdentity<AppUserModel, IdentityRole>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

        // Cookie config (fix Correlation failed)
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax; // Cho phép OAuth Google hoạt động
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ⚠️ Bắt buộc khi SameSite=None
        });

        // Google Authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        })
        .AddCookie()
        .AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
            googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            googleOptions.CallbackPath = "/signin-google";
        });

        // Razor Pages (nếu cần)
        builder.Services.AddRazorPages();

        // Identity rules
        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 4;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        });

        var app = builder.Build();

        app.UseStatusCodePagesWithRedirects("/Home/Error?statuscode={0}");

        app.UseStaticFiles();

        app.UseRouting();

        app.UseSession();            // ✅ Session cần trước Authentication
        app.UseAuthentication();     // ✅ Authentication
        app.UseAuthorization();      // ✅ Gọi đúng 1 lần

        // Define Routes
        app.MapControllerRoute(
            name: "Areas",
            pattern: "{area:exists}/{controller=Product}/{action=Index}/{id?}");

        app.MapControllerRoute(
            name: "category",
            pattern: "category/{Slug?}",
            defaults: new { controller = "Brand", action = "Index" });

        app.MapControllerRoute(
            name: "brand",
            pattern: "brand/{Slug?}",
            defaults: new { controller = "Category", action = "Index" });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // Seeding
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<DataContext>();
            shopping_tutorial.Repository.SeedData.seedingData(context);
            shopping_tutorial.Data.SeedData.SeedColorsAndSizesSync(context);
        }

        app.Run();
    }
}