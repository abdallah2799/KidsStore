using Application.Interfaces.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;

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

// 🔹 Proper Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
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

app.MapStaticAssets();
app.UseRouting();

// 🔹 Correct order: Session → Authentication → Authorization
app.UseSession();
// Rehydrate session from cookies
app.Use(async (context, next) =>
{
    var session = context.Session;
    var cookies = context.Request.Cookies;

    if (session.GetInt32("UserId") is null &&
        cookies.TryGetValue("UserId", out var userId) &&
        cookies.TryGetValue("UserName", out var userName) &&
        cookies.TryGetValue("UserRole", out var userRole))
    {
        session.SetInt32("UserId", int.Parse(userId));
        session.SetString("UserName", userName);
        session.SetString("UserRole", userRole);
    }

    await next();
});

// Refresh cookies if active
app.Use(async (context, next) =>
{
    var cookies = context.Request.Cookies;
    if (cookies.ContainsKey("UserId"))
    {
        var opts = new CookieOptions
        {
            Expires = DateTime.UtcNow.AddDays(7),
            HttpOnly = true,
            Secure = false
        };
        context.Response.Cookies.Append("UserId", cookies["UserId"]!, opts);
        context.Response.Cookies.Append("UserName", cookies["UserName"]!, opts);
        context.Response.Cookies.Append("UserRole", cookies["UserRole"]!, opts);
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 6. Routing
// ==========================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

// ==========================================
// 7. Database Migration & Default Seeding
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Log.Information("Seeding started");

    context.Database.Migrate(); // Ensure DB & schema exist

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
