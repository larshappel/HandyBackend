namespace HandyBackend.DTOs;

// This class will define validation rules for the delivery record.
public class DeliveryRecordDto
{
    public int Product_Id { get; set; }
    public double Amount { get; set; }
    public string individual_id { get; set; } = string.Empty;
}
