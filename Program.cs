using Microsoft.EntityFrameworkCore;
using ITWebManagement.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging to file
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/app-{Date}.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register ApplicationDbContext with DI
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication with cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSerilog(Log.Logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            logger.LogError(exceptionHandlerPathFeature.Error, "Unhandled exception occurred at {Path}", exceptionHandlerPathFeature.Path);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Lỗi không được xử lý: {exceptionHandlerPathFeature.Error.Message}");
        }
        else
        {
            logger.LogError("Bad Request at {Path}", context.Request.Path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Yêu cầu không hợp lệ. Vui lòng kiểm tra dữ liệu gửi lên.");
        }
    });
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Sửa đoạn mã MapControllerRoute
app.MapControllerRoute(
    name: "default", // Khai báo tham số name
    pattern: "{controller=Devices}/{action=Index}/{id?}" // Khai báo tham số pattern
);

app.Run();