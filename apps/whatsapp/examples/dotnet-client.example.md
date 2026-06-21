# .NET / C# Integration Example

## Configuration (appsettings.json)

```json
{
  "WhatsApp": {
    "Enabled": true,
    "ServiceUrl": "http://localhost:4000",
    "InternalApiKey": "your-32-char-key-here",
    "TimeoutSeconds": 10
  }
}
```

## HTTP Client Service

```csharp
public interface IWhatsAppService
{
    Task<bool> SendMessageAsync(string phone, string message);
    Task<WhatsAppStatus> GetStatusAsync();
    Task<bool> HealthCheckAsync();
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config["WhatsApp:ServiceUrl"]);
        _httpClient.DefaultRequestHeaders.Add("x-internal-api-key", config["WhatsApp:InternalApiKey"]);
        _httpClient.Timeout = TimeSpan.FromSeconds(int.Parse(config["WhatsApp:TimeoutSeconds"] ?? "10"));
        _logger = logger;
    }

    public async Task<bool> SendMessageAsync(string phone, string message)
    {
        try
        {
            var body = new { phone, message };
            var response = await _httpClient.PostAsJsonAsync("/send-message", body);
            var result = await response.Content.ReadFromJsonAsync<SendResult>();
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp send failed for {Phone}", MaskPhone(phone));
            return false;
        }
    }

    public async Task<WhatsAppStatus> GetStatusAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<WhatsAppStatus>("/status");
        return response;
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<HealthResult>("/health");
            return response?.Ok == true;
        }
        catch
        {
            return false;
        }
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 6) return "****";
        return phone[..2] + new string('*', phone.Length - 6) + phone[^4..];
    }

    private record SendResult(bool Success, string Error);
    private record HealthResult(bool Ok, string Service, string WhatsAppState);
    private record WhatsAppStatus(string State, bool QrAvailable, string LastSentAt, string Error);
}
```

## Registration (Program.cs)

```csharp
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();
```

## Usage

```csharp
public class OrderService
{
    private readonly IWhatsAppService _whatsApp;

    public OrderService(IWhatsAppService whatsApp) => _whatsApp = whatsApp;

    public async Task NotifyCustomerAsync(string phone, string orderNumber)
    {
        var message = $"Order #{orderNumber} has been confirmed!";
        var sent = await _whatsApp.SendMessageAsync(phone, message);
        if (!sent)
        {
            _logger.LogWarning("WhatsApp notification queued for retry");
        }
    }
}
```
