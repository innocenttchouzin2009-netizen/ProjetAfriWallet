using System.Text.Json;

namespace AfriWallet.Banking.Api.Production.Logging;

public sealed class BankingStructuredLogger
{
    private readonly ILogger<BankingStructuredLogger> _logger;

    public BankingStructuredLogger(ILogger<BankingStructuredLogger> logger)
    {
        _logger = logger;
    }

    public void LogEvent(string eventName, string? correlationId = null, string? traceId = null, string? workflowId = null, string? intentId = null, string? executionId = null, object? data = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["event"] = eventName,
            ["correlationId"] = correlationId,
            ["traceId"] = traceId,
            ["workflowId"] = workflowId,
            ["intentId"] = intentId,
            ["executionId"] = executionId,
            ["data"] = data is null ? null : JsonSerializer.Serialize(data)
        };

        _logger.LogInformation("banking-event {Payload}", JsonSerializer.Serialize(payload));
    }
}
