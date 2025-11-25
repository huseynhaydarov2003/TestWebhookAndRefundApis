namespace TestWebhooAndRefundApis.Models;

public class PaymentResultData
{
    public string SessionNumber { get; set; }
    public long PaymentNumber { get; set; }
    public string PaymentStatus { get; set; }
}