namespace HandyBackend.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public double Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdateDate { get; set; }
    public TimeSpan? UpdateTime { get; set; }
}
