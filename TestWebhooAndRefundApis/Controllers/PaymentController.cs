using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using TestWebhooAndRefundApis.Models;

namespace TestWebhooAndRefundApis.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _config;

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
}
