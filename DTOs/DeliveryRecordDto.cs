namespace HandyBackend.DTOs;

// This class will define validation rules for the delivery record.
public class DeliveryRecordDto
{
    public string product_id { get; set; }
    public double amount { get; set; }
    public string individual_id { get; set; } = string.Empty;
}
