namespace TestWebhooAndRefundApis.Models;

public class PaymentRefundRequest
{
    public string Hash { get; set; } 
    public decimal Amount { get; set; }
    public string PaymentNumber { get; set; }
    public string SessionNumber { get; set; }
    public string CurrencyIsoNumber { get; set; }
}