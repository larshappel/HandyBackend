namespace HandyBackend.DTOs;

// This class will define validation rules for the delivery record.
public class DeliveryRecordDto
{
    public string Product_Id { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string individual_id { get; set; } = string.Empty;
}

