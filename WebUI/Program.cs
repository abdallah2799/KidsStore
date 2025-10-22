using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Configure Serilog (with SQL + File + Console)
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
            AutoCreateSqlTable = true // <--- this automatically creates the table
        },
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        columnOptions: columnOptions
    )
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================================
// 2. Services
// ==========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

// ==========================================
// 3. App
// ==========================================
var app = builder.Build();

// ==========================================
// 4. Middleware
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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

app.Run();

Log.CloseAndFlush();
