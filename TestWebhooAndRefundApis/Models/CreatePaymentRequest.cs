namespace TestWebhooAndRefundApis.Models;

public class CreatePaymentRequest
{
    public string Hash { get; set; }
    public string PaymentMethodCode { get; set; }
    public string SessionNumber { get; set; }
    public Guid PosId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyIsoNumber { get; set; }
    public string AccountNumber { get; set; }
    public long UserMsisdn { get; set; }
    public Guid UserId { get; set; }
    public string AccountTypeCode { get; set; }
    public string UserFullName { get; set; }
    public string InvoiceNumber { get; set; }
    public List<ParamItem>? Params { get; set; }
}

public class ParamItem
{
    public string Key { get; set; }
    public string Value { get; set; }
}
