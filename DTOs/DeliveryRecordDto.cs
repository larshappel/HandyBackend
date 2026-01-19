namespace HandyBackend.DTOs;

// This class will define validation rules for the delivery record.
public class DeliveryRecordDto
{
    public string product_id { get; set; } = string.Empty;
    public string amount { get; set; } = string.Empty;
    public string individual_id { get; set; } = string.Empty;
    public string device_id { get; set; } = string.Empty;
}
