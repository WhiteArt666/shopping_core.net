using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Areas.Admin.Repository;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using shopping_tutorial.Data;

internal class Program
{
    private static void Main(string[] args)
    {
        
        var builder = WebApplication.CreateBuilder(args);

        //connection db
        // builder.Services.AddDbContext<DataContext>(options =>
        // {
        //     options.UseSqlServer(builder.Configuration["ConnectionStrings:ConnectDb"]);
        // });
       builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});


        //Add Email Sender
        builder.Services.AddTransient<IEmailSender, EmailSender>();

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(15);
            options.Cookie.IsEssential = true;
        });
        //Khai báo Identity 
        builder.Services.AddIdentity<AppUserModel, IdentityRole>().AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders();

        builder.Services.AddRazorPages();

        builder.Services.Configure<IdentityOptions>(options =>
        {
            // Password settings.
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 4;


            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            //User settings 
            options.User.RequireUniqueEmail = true; // một email chỉ có 1 tài khoàn duy nhất 
        });

        var app = builder.Build();
        app.UseStatusCodePagesWithRedirects("/Home/Error?statuscode={0}");
        app.UseSession();

        app.UseStaticFiles();
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }
        else
        {
            app.UseDeveloperExceptionPage(); // ✅ dòng này giúp hiện lỗi chi tiết khi chạy ở Development
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();// đăng nhập 
        app.UseAuthorization(); // xác thực xem account có quyền gì 

        app.MapControllerRoute(
               name: "Areas",
               pattern: "{area:exists}/{controller=Product}/{action=Index}/{id?}");

        app.UseAuthorization();
        app.MapControllerRoute(
               name: "category",
               pattern: "category/{Slug?}",
        defaults: new { controller = "Brand", action = "Index" });

        app.UseAuthorization();
        app.MapControllerRoute(
               name: "brand",
               pattern: "brand/{Slug?}",
        defaults: new { controller = "Category", action = "Index" });

        app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

        //seeding data
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<DataContext>();
            shopping_tutorial.Repository.SeedData.seedingData(context);
            
            // Seed Colors and Sizes
            shopping_tutorial.Data.SeedData.SeedColorsAndSizesSync(context);
        }

        //var context =app.Services.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        //      SeedData.seedingData(context);


        app.Run();

    }
}
