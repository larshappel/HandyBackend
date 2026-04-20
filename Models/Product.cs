namespace HandyBackend.Models;

public class Product
{
    public int Id { get; set; }
    public int OrderDetailId { get; set; }
    public long? IdentificationNumber { get; set; }
    public double Amount { get; set; }
    public int LabelIssueCount;
    public int LabelCollectCount;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdateDate { get; set; }
    public TimeSpan? UpdateTime { get; set; }
}
