using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TestWebhooAndRefundApis.Models;

namespace TestWebhooAndRefundApis.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _config;
    private static readonly HttpClient Client = new();

    public PaymentController(ILogger<PaymentController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [HttpPost("webhook")]
    public IActionResult PaymentWebhook([FromBody] PaymentWebhookRequest request)
    {
        _logger.LogInformation("Webhook received: {@Request}", request);

        var secretKey = _config["SecretKey:Value"];
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("Missing secret key in configuration.");
            return StatusCode(500, "Missing secret key in configuration");
        }

        var isValid = VerifyHashWebHookRequest(request, secretKey, _logger);

        if (!isValid)
        {
            _logger.LogWarning("Invalid hash for payment {PaymentNumber}", request.PaymentNumber);
            throw new InvalidDataException();
        }

        return Ok();
    }

    [HttpPost("refund")]
    public IActionResult PaymentRefund([FromBody] PaymentRefundRequest request)
    {
        _logger.LogInformation("Payment refund received: {@Request}", request);

        var secretKey = _config["SecretKey:Value"];

        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("Missing secret key in configuration.");
            return StatusCode(500, "Missing secret key in configuration");
        }

        var isValid = VerifyHashRefundRequest(request, secretKey, _logger);

        if (!isValid)
        {
            _logger.LogWarning("Invalid hash for payment {PaymentNumber}", request.PaymentNumber);
            throw new InvalidDataException();
        }

        return Ok();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        _logger.LogInformation("Create payment received: {@Request}", request);

        var secretKey = _config["SecretKey:Value"] ?? string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("Missing secret key in configuration.");
            return StatusCode(500, "Missing secret key in configuration");
        }

        var isValid = VerifyHashCreatePaymentRequest(request, secretKey, _logger);
       
        if (!isValid)
        {
            _logger.LogWarning("Invalid hash for payment {SessionNumber}", request.SessionNumber);
         
            return BadRequest("Ошибка проверки контроля суммы пакета");
        }

        var baseUrl = _config["BaseUrl:Value"];
       
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogError("BaseUrl is missing in configuration.");
            return StatusCode(500, "BaseUrl is not configured.");
        }

        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var apiKey = _config["ApiKey:Value"] ?? string.Empty;
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Missing API key in configuration.");
                return StatusCode(500, "Missing API key in configuration.");
            }
            
            string apiKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
            
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("X-API-KEY", apiKeyBase64);
            
            var httpResponse = await Client.PostAsync($"{baseUrl}/merchant/external/api/v1/payments/create", content);

            if (!httpResponse.IsSuccessStatusCode)
                return StatusCode((int)httpResponse.StatusCode, "Failed to create payment on server.");
            
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            ApiResponse<PaymentResultData> serverResponse =
                JsonSerializer.Deserialize<ApiResponse<PaymentResultData>>(responseBody, options)
                ?? throw new Exception("Failed to parse server response.");
            
            return Ok(serverResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating payment via external API.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckPayment([FromBody] CheckPaymentRequest request)
    {
    _logger.LogInformation("Check payment received: {@Request}", request);

    var secretKey = _config["SecretKey:Value"] ?? string.Empty;
    if (string.IsNullOrEmpty(secretKey))
    {
        _logger.LogError("Missing secret key in configuration.");
        return StatusCode(500, "Missing secret key in configuration");
    }

    var isValid = VerifyHashCheckPaymentRequest(request, secretKey, _logger);
    
    if (!isValid)
    {
        _logger.LogWarning("Invalid hash for session {SessionNumber}", request.SessionNumber);
      
        return BadRequest("Ошибка проверки контроля суммы пакета");
    }

    var baseUrl = _config["BaseUrl:Value"];
   
    if (string.IsNullOrEmpty(baseUrl))
    {
        _logger.LogError("BaseUrl is missing in configuration.");
        return StatusCode(500, "BaseUrl is not configured.");
    }

    try
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
      
        var apiKey = _config["ApiKey:Value"] ?? string.Empty;
            
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Missing API key in configuration.");
            return StatusCode(500, "Missing API key in configuration.");
        }
            
        string apiKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
            
        Client.DefaultRequestHeaders.Clear();
        Client.DefaultRequestHeaders.Add("X-API-KEY", apiKeyBase64);
        
        var httpResponse = await Client.PostAsync($"{baseUrl}/merchant/external/api/v1/payments/check", content);

        if (!httpResponse.IsSuccessStatusCode)
            return StatusCode((int)httpResponse.StatusCode, "Failed to check payment on server.");

        string responseBody = await httpResponse.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        ApiResponse<PaymentResultData> serverResponse =
            JsonSerializer.Deserialize<ApiResponse<PaymentResultData>>(responseBody, options)
            ?? throw new Exception("Failed to parse server response.");

        return Ok(serverResponse);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error while checking payment via external API.");
        return StatusCode(500, "Internal server error.");
    } 
    }


    private static bool VerifyHashWebHookRequest(PaymentWebhookRequest req, string secretKey, ILogger logger)
    {
        string amountFormatted = req.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        
        string rawData = $"{amountFormatted}{req.PaymentNumber}{req.PaymentStatus}{req.SessionNumber}.{secretKey}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
      
        logger.LogInformation("RawData: {RawData}", rawData);
        logger.LogInformation("ComputedHash: {ComputedHash}", computedHash);
        logger.LogInformation("ReceivedHash: {ReceivedHash}", req.Hash);
        
        return string.Equals(computedHash, req.Hash, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VerifyHashRefundRequest(PaymentRefundRequest req, string secretKey, ILogger logger)
    {
        string amountFormatted = req.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        string rawData = $"{amountFormatted}{req.PaymentNumber}{req.SessionNumber}{req.CurrencyIsoNumber}.{secretKey}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        
        logger.LogInformation("RawData: {RawData}", rawData);
        logger.LogInformation("ComputedHash: {ComputedHash}", computedHash);
        logger.LogInformation("ReceivedHash: {ReceivedHash}", req.Hash);
        
        return string.Equals(computedHash, req.Hash, StringComparison.OrdinalIgnoreCase);
    }
    
    public static bool VerifyHashCreatePaymentRequest(CreatePaymentRequest req, string secretKey, ILogger logger)
    {
        string amountFormatted = req.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        string rawData = string.Concat(
            req.AccountNumber,
            req.AccountTypeCode,
            amountFormatted,
            req.CurrencyIsoNumber,
            req.InvoiceNumber,
            req.PaymentMethodCode,
            req.PosId,
            req.SessionNumber,
            req.UserFullName,
            req.UserId,
            req.UserMsisdn,
            ".",
            secretKey
        );

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        
        logger.LogInformation("RawData: {RawData}", rawData);
        logger.LogInformation("ComputedHash: {ComputedHash}", computedHash);
        logger.LogInformation("ReceivedHash: {ReceivedHash}", req.Hash);
        
        return string.Equals(computedHash, req.Hash, StringComparison.OrdinalIgnoreCase);
    }
    
    public static bool VerifyHashCheckPaymentRequest(CheckPaymentRequest req, string secretKey, ILogger logger)
    {
        string amountFormatted = req.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        string rawData = string.Concat(amountFormatted, req.SessionNumber, ".", secretKey);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        
        logger.LogInformation("RawData: {RawData}", rawData);
        logger.LogInformation("ComputedHash: {ComputedHash}", computedHash);
        logger.LogInformation("ReceivedHash: {ReceivedHash}", req.Hash);
        
        return string.Equals(computedHash, req.Hash, StringComparison.OrdinalIgnoreCase);
    }
}
