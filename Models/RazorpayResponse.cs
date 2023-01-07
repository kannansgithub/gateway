namespace MyCart.Payment.Models;
public class RazorpayResponse
{
    public string? entity { get; set; }
    public string? account_id { get; set; }
    public string? events { get; set; }
    public List<string>? contains { get; set; }
    public PaymentInfo? payload { get; set; }
    public long created_at { get; set; }
}