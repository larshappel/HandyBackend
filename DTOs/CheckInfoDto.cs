namespace HandyBackend.DTOs;

public class CheckInfoDto
{
    public string Scheme { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DestinationUrl => string.IsNullOrEmpty(Scheme) || string.IsNullOrEmpty(Host)
        ? string.Empty
        : Port > 0
            ? $"{Scheme}://{Host}:{Port}"
            : $"{Scheme}://{Host}";
    public string MachineName { get; set; } = string.Empty;
    public IReadOnlyList<string> LocalIpv4Addresses { get; set; } = Array.Empty<string>();
}
