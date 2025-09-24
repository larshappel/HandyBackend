using HandyBackend.Data;
using HandyBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;

var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = WindowsServiceHelpers.IsWindowsService()
            ? AppContext.BaseDirectory
            : default,
    }
);

builder.Host.UseWindowsService();

// Listen on all network interfaces, necessary to access from other machines.
builder.WebHost.UseUrls("http://*:5024", "https://*:7024");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        // "DefaultConnection" is the name of the ConnectionString for local testing.
        // ServerConnection is for staging.
        // These strings aren't part of the Git Repo so take care of them
        builder.Configuration.GetConnectionString("ServerConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ServerConnection"))
    )
);

// Add Services for dependency injection
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Add a global exception handler.
// Don't forget: This will catch all exceptions and hide them behind a generic error message.
// Only console output can reveal more details about the exact error (such as MYSQL connection trouble)
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(
            new { error = "An unexpected server error occurred." }
        );
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HttpsRedirection is necessary for HTTPS in production environments.
// TODO: Manage the HTTPS certificates (the frontend must also be changed) and
// make it more secure by using HTTPS. However now, as the data isn't sensitive
// we'll use HTTP.
//
// app.UseHttpsRedirection();

/*
 * Weather Forecast Test Route
 * for a basic connectivity test
 */
var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching",
};

app.MapGet(
        "/weatherforecast",
        () =>
        {
            var forecast = Enumerable
                .Range(1, 5)
                .Select(index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            return forecast;
        }
    )
    .WithName("GetWeatherForecast")
    .WithOpenApi();

/*
 * End of Weather Forecast
 */

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
