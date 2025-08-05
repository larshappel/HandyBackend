using System.Text.Json.Serialization;

namespace HandyBackend.Models.DTOs;

public class ProductUpdateDto
{
    public int? OrderDetailId { get; set; }
    
    [JsonPropertyName("individual_id")]
    public int? IndividualId { get; set; }
    
    // public decimal? Price { get; set; }
    public double? Amount { get; set; }
}

