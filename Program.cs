using HandyBackend.Data;
using HandyBackend.Middleware;
using HandyBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using Serilog.Events;

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(
        new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = WindowsServiceHelpers.IsWindowsService()
                ? AppContext.BaseDirectory
                : default,
        }
    );

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        // Default sink for general logs
        .WriteTo.Logger(lc => lc
            .Filter.ByExcluding(e => e.Properties.ContainsKey("LogType") && e.Properties["LogType"] is ScalarValue sv && sv.Value as string == "ClientAccess")
            .WriteTo.File("logs/backend-log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10))
        // Sink for client-accessible logs
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("LogType") && e.Properties["LogType"] is ScalarValue sv && sv.Value as string == "ClientAccess")
            .WriteTo.File("logs/client-access-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)));

    builder.Host.UseWindowsService();

    // Listen on all network interfaces, necessary to access from other machines.
    builder.WebHost.UseUrls("http://*:5000");

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddControllers();

    // Add DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
        )
    );

    // Add Services for dependency injection
    builder.Services.AddScoped<IProductService, ProductService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Use the global exception handler middleware
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // HttpsRedirection is necessary for HTTPS in production environments.
    app.UseHttpsRedirection();

    // Use the custom middleware to log raw request bodies
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during application startup");
}
finally
{
    Log.CloseAndFlush();
}
