using HandyBackend.Data;
using HandyBackend.Logging;
using HandyBackend.Middleware;
using HandyBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

int DEFAULT_LOG_COUNT = 10;
int DEFAULT_PORT = 5000;

// Configure Serilog for logging and guard against file sink failures
Log.Logger = CreateBootstrapLogger(DEFAULT_LOG_COUNT);

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

    builder.Host.UseSerilog(
        (context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .WriteTo.Console();

            TryConfigureGeneralLogSink(loggerConfiguration, DEFAULT_LOG_COUNT);
            TryConfigureClientAccessSink(
                loggerConfiguration,
                context.Configuration,
                DEFAULT_LOG_COUNT
            );
        }
    );

    builder.Host.UseWindowsService();

    // Listen on all network interfaces, necessary to access from other machines.
    var port = builder.Configuration.GetValue<int>("Hosting:Port", DEFAULT_PORT);
    builder.WebHost.UseUrls($"http://*:{port}");

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
    try
    {
        Log.CloseAndFlush();
    }
    catch (Exception flushException)
    {
        Console.Error.WriteLine($"Serilog failed to flush: {flushException}");
    }
}

// Default logger
Serilog.ILogger CreateBootstrapLogger(int retainedFileCount)
{
    var configuration = new LoggerConfiguration().WriteTo.Console();

    TryConfigureBootstrapFileSink(configuration, retainedFileCount);

    return configuration.CreateBootstrapLogger();
}

void TryConfigureBootstrapFileSink(LoggerConfiguration configuration, int retainedFileCount)
{
    try
    {
        configuration.WriteTo.File(
            "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: retainedFileCount
        );
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Serilog bootstrap file sink disabled: {ex}");
    }
}

void TryConfigureGeneralLogSink(LoggerConfiguration configuration, int retainedFileCount)
{
    try
    {
        configuration.WriteTo.Logger(lc =>
            lc.Filter.ByExcluding(e =>
                    e.Properties.ContainsKey("LogType")
                    && e.Properties["LogType"] is ScalarValue sv
                    && sv.Value as string == "ClientAccess"
                )
                .WriteTo.File(
                    "logs/backend-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedFileCount
                )
        );
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Serilog backend log sink disabled: {ex}");
    }
}

void TryConfigureClientAccessSink(
    LoggerConfiguration configuration,
    IConfiguration appConfiguration,
    int retainedFileCount
)
{
    try
    {
        configuration.WriteTo.Logger(lc =>
            lc.Filter.ByIncludingOnly(e =>
                    e.Properties.ContainsKey("LogType")
                    && e.Properties["LogType"] is ScalarValue sv
                    && sv.Value as string == "ClientAccess"
                )
                .WriteTo.File(
                    appConfiguration.GetValue<string>("Logging:CustomLogger:ClientAccessLogPath")
                        ?? "logs/client-access-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: appConfiguration.GetValue(
                        "Logging:CustomLogger:ClientAccessLogCount",
                        retainedFileCount
                    ),
                    hooks: new CsvHeaderHooks()
                )
        );
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Serilog client access sink disabled: {ex}");
    }
}
