namespace HandyBackend.Models.DTOs;

public class ProductUpdateDto
{
    public int? OrderDetailId { get; set; }

    // public decimal? Price { get; set; }
    public double? Amount { get; set; }
    public int? IndividualId { get; set; }
}
