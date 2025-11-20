namespace TestWebhooAndRefundApis.Models;

public class PaymentWebhookRequest
{
    public string Hash { get; set; } 
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; }
    public string PaymentNumber { get; set; }
    public string SessionNumber { get; set; }
}