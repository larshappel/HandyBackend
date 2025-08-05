namespace HandyBackend.Models.DTOs;

public class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;

    // public decimal Price { get; set; }
    public double Amount { get; set; }
}

