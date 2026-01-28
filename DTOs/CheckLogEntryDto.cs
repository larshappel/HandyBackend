namespace HandyBackend.DTOs;

public class CheckLogEntryDto
{
    public DateTimeOffset Timestamp { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string IndividualId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
