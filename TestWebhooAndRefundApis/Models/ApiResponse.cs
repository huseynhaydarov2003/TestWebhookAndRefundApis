namespace TestWebhooAndRefundApis.Models;

public class ApiResponse<T>
{
    public bool Status { get; set; }
    public int Code { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}