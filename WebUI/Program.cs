using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Configure Serilog (File + Console only at startup)
// ==========================================
// Note: SQL Server logging will be enabled after database is created
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================================
// 2. Core Services Registration
// ==========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseLazyLoadingProxies()); // Enable lazy loading

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<Application.Interfaces.Services.IReportService, Application.Services.ReportService>();
builder.Services.AddScoped<Application.Interfaces.Services.IUserService, Application.Services.UserService>();
builder.Services.AddScoped<Application.Interfaces.Services.IBackupService, Application.Services.BackupService>();

// ==========================================
// 3. Session & Authentication Configuration
// ==========================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Proper Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Remember Me
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnSigningIn = context =>
            {
                // Log sign-in event (optional)
                Log.Information($"User '{context?.Principal?.Identity?.Name}' signed in.");
                return Task.CompletedTask;
            }
        };
    });

// ✅ Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CashierOnly", policy => policy.RequireRole("Cashier"));
    options.AddPolicy("AdminOrCashier", policy => policy.RequireRole("Admin", "Cashier"));

});

// ==========================================
// 4. Build App
// ==========================================
var app = builder.Build();

// ==========================================
// 5. Middleware Pipeline (⚠️ Order Matters)
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapStaticAssets();
app.UseRouting();

// ✅ Correct order: Session → Authentication → Authorization
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 6. Routing
// ==========================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
).WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Ensure database is created and migrations are applied
    Log.Information("Ensuring database is created and up to date");
    await context.Database.MigrateAsync();
    Log.Information("Database is ready");
    
    Log.Information("Seeding data");
    await Infrastructure.Persistence.DbSeeder.SeedAsync(context);
    Log.Information("Seeding finished, starting app");
}

// ==========================================
// 8. Run App
// ==========================================
app.Run();

Log.Information("Exiting app");
Log.CloseAndFlush();
