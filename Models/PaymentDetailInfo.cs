namespace MyCart.Payment.Models;

public class PaymentDetailInfo
{
    public string? id { get; set; }
    public double amount { get; set; }
    public string? currency { get; set; }
    public string? status { get; set; }
    public string? order_id { get; set; }
}