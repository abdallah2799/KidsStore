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
// 1. Configure Serilog (SQL + File + Console)
// ==========================================
var columnOptions = new ColumnOptions
{
    AdditionalColumns = new Collection<SqlColumn>
    {
        new SqlColumn("UserName", System.Data.SqlDbType.NVarChar, dataLength: 256),
        new SqlColumn("RequestPath", System.Data.SqlDbType.NVarChar, dataLength: 512),
        new SqlColumn("SourceContext", System.Data.SqlDbType.NVarChar, dataLength: 256),
        new SqlColumn("MachineName", System.Data.SqlDbType.NVarChar, dataLength: 128)
    }
};

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "Logs",
            AutoCreateSqlTable = true
        },
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        columnOptions: columnOptions
    )
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================================
// 2. Core Services Registration
// ==========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IVendorService, VendorService>();

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

// ==========================================
// 7. Database Migration & Default Seeding
// ==========================================
// Seeder function
async Task SeedVendorsAsync(AppDbContext context)
{
    if (await context.Vendors.AnyAsync())
    {
        Console.WriteLine("الفيندورز موجودة بالفعل، لن يتم إضافة بيانات جديدة.");
        return;
    }

    var vendors = new List<Vendor>
    {
        new Vendor { Name = "المورد ألفا", CodePrefix = "MA", Address = "القاهرة، مصر", ContactInfo = "01000000001", Notes = "أفضل مورد للأجهزة الإلكترونية" },
        new Vendor { Name = "المورد بيتا", CodePrefix = "MB", Address = "الإسكندرية، مصر", ContactInfo = "01000000002", Notes = "مورد مستلزمات مكتبية" },
        new Vendor { Name = "المورد جاما", CodePrefix = "MG", Address = "الجيزة، مصر", ContactInfo = "01000000003", Notes = "لديه خبرة 10 سنوات" },
        new Vendor { Name = "المورد دلتا", CodePrefix = "MD", Address = "طنطا، مصر", ContactInfo = "01000000004", Notes = "" },
        new Vendor { Name = "المورد إبسلون", CodePrefix = "ME", Address = "منصورة، مصر", ContactInfo = "01000000005", Notes = "سريع في التسليم" },
        new Vendor { Name = "المورد زيتا", CodePrefix = "MZ", Address = "الإسماعيلية، مصر", ContactInfo = "01000000006", Notes = "" },
        new Vendor { Name = "المورد إيتا", CodePrefix = "MET", Address = "السويس، مصر", ContactInfo = "01000000007", Notes = "" },
        new Vendor { Name = "المورد ثيتا", CodePrefix = "MTH", Address = "شرم الشيخ، مصر", ContactInfo = "01000000008", Notes = "مورد سياحي وتجهيز الفنادق" },
        new Vendor { Name = "المورد يوتا", CodePrefix = "MI", Address = "الغردقة، مصر", ContactInfo = "01000000009", Notes = "" },
        new Vendor { Name = "المورد كابا", CodePrefix = "MK", Address = "الأقصر، مصر", ContactInfo = "01000000010", Notes = "يقدم خصومات عند الشراء بالجملة" }
    };

    await context.Vendors.AddRangeAsync(vendors);
    await context.SaveChangesAsync();

    Console.WriteLine("تم إضافة 10 موردين بنجاح.");
}
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Log.Information("Seeding started");
    await SeedVendorsAsync(context);
    context.Database.Migrate();

    if (!context.Users.Any())
    {
        var admin = new Domain.Entities.User
        {
            UserName = "admin",
            Role = Domain.Entities.UserRole.Admin,
            IsActive = true
        };
        admin.SetPassword("admin123");

        var cashier = new Domain.Entities.User
        {
            UserName = "cashier",
            Role = Domain.Entities.UserRole.Cashier,
            IsActive = true
        };
        cashier.SetPassword("cashier123");

        context.Users.AddRange(admin, cashier);
        context.SaveChanges();

        Log.Information("Default users seeded: admin / cashier");
    }
    else
    {
        Log.Information("Users already exist — skipping seeding");
    }

    Log.Information("Seeding finished, starting app");
}

// ==========================================
// 8. Run App
// ==========================================
app.Run();

Log.Information("Exiting app");
Log.CloseAndFlush();
