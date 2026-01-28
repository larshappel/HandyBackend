namespace HandyBackend.DTOs;

public class ProductSnapshotDto
{
    public int OrderDetailId { get; set; }
    public double Amount { get; set; }
    public int LabelCollectCount { get; set; }
    public long? IdentificationNumber { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
