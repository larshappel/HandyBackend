using System.IO;
using HandyBackend.Data;
using HandyBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HandyBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckController : ControllerBase
{
    private const int MaxLogEntries = 50;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CheckController> _logger;

    public CheckController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<CheckController> logger
    )
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetClientAccessLogs()
    {
        var logFile = ResolveLatestClientAccessLogPath();
        if (string.IsNullOrEmpty(logFile) || !System.IO.File.Exists(logFile))
        {
            return Ok(new
            {
                Source = logFile,
                Entries = Array.Empty<CheckLogEntryDto>(),
                Message = "Client access log not found"
            });
        }

        var lines = await System.IO.File.ReadAllLinesAsync(logFile);
        var entries = new List<CheckLogEntryDto>();
        foreach (var line in lines.Reverse())
        {
            if (entries.Count >= MaxLogEntries)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("Timestamp", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(',', 5);
            if (parts.Length < 5)
                continue;

            var entry = new CheckLogEntryDto
            {
                Timestamp = DateTimeOffset.TryParse(parts[0].Trim(), out var ts) ? ts : DateTimeOffset.MinValue,
                ProductId = parts[1].Trim(),
                Amount = parts[2].Trim(),
                IndividualId = parts[3].Trim(),
                Message = parts[4].Trim(),
            };

            entries.Add(entry);
        }

        entries.Reverse();
        return Ok(new
        {
            Source = Path.GetFullPath(logFile),
            Entries = entries,
        });
    }

    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<ProductSnapshotDto>>> GetProductSnapshots()
    {
        try
        {
            var snapshots = await _context.Products!
                .AsNoTracking()
                .OrderByDescending(p => p.UpdateDate)
                .ThenByDescending(p => p.UpdateTime)
                .Take(50)
                .Select(p => new ProductSnapshotDto
                {
                    OrderDetailId = p.OrderDetailId,
                    Amount = p.Amount,
                    LabelCollectCount = p.LabelCollectCount,
                    IdentificationNumber = p.IdentificationNumber,
                    UpdatedAt = p.UpdateDate.HasValue && p.UpdateTime.HasValue
                        ? p.UpdateDate.Value.Add(p.UpdateTime.Value)
                        : (DateTime?)null
                })
                .ToListAsync();

            return Ok(snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch product snapshots for check UI.");
            return Problem(
                detail: $"Product snapshot取得エラー: {ex.Message}",
                statusCode: (int)HttpStatusCode.InternalServerError
            );
        }
    }

    [HttpGet("info")]
    public IActionResult GetDestinationInfo()
    {
        var scheme = Request.Scheme;
        var host = Request.Host.Host;
        var port = Request.Host.Port ?? 0;

        var ipv4s = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Select(unicast => unicast.Address)
            .Where(addr => addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
            .Select(addr => addr.ToString())
            .Distinct()
            .ToList();

        var info = new CheckInfoDto
        {
            Scheme = scheme,
            Host = string.IsNullOrEmpty(host) ? Environment.MachineName : host,
            Port = port,
            MachineName = Environment.MachineName,
            LocalIpv4Addresses = ipv4s
        };

        return Ok(info);
    }

    private string? ResolveLatestClientAccessLogPath()
    {
        var configuredPath = _configuration["Logging:CustomLogger:ClientAccessLogPath"] ?? "logs/client-access-.txt";
        var directory = Path.GetDirectoryName(configuredPath);
        var folder = string.IsNullOrEmpty(directory)
            ? Path.Combine(Directory.GetCurrentDirectory(), "logs")
            : Path.IsPathFullyQualified(directory)
                ? directory
                : Path.Combine(Directory.GetCurrentDirectory(), directory);

        if (!Directory.Exists(folder))
        {
            _logger.LogWarning("Client access log folder does not exist: {Folder}", folder);
            return null;
        }

        var fileName = Path.GetFileName(configuredPath);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var searchPattern = string.IsNullOrEmpty(baseName) ? "client-access-*.txt" : $"{baseName}*.txt";

        var candidate = Directory
            .EnumerateFiles(folder, searchPattern)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .Select(info => info.FullName)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(candidate))
            return candidate;

        var fallback = Path.Combine(folder, fileName);
        return System.IO.File.Exists(fallback) ? fallback : null;
    }
}
