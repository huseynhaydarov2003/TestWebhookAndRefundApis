namespace TestWebhooAndRefundApis.Models;

public class CheckPaymentRequest
{
    public string Hash { get; set; }
    public string SessionNumber { get; set; }
    public decimal Amount { get; set; }
}