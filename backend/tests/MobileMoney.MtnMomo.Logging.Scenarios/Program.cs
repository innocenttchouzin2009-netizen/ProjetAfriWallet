using System.Text.Json;
using MobileMoney.Production.Correlation;
using MobileMoney.Production.Logging;

var correlation = new CorrelationContext("trace-123", "txn-456", "ref-789", "awid-123", "wallet-999", "mtn-momo-cm", "DEPOSIT");
var logger = new StructuredOperationLogger();
var scope = new MobileMoneyLoggingScope(correlation);

var messages = new List<string>();
using (logger.BeginScope(scope))
{
    logger.LogRequestStarted("MTN_MOMO_REQUEST_STARTED", correlation, 100, "XAF", "237670000000");
    logger.LogRequestAccepted("MTN_MOMO_REQUEST_ACCEPTED", correlation, 100, "XAF", "237670000000");
}

var redacted = SensitiveDataRedactor.Redact("Authorization: Bearer abc123\nApiKey=secret\nPhone=237670000000");
if (redacted.Contains("abc123", StringComparison.OrdinalIgnoreCase) || redacted.Contains("secret", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Sensitive values should be redacted from logs.");
}

var sanitized = CorrelationIdValidator.Normalize("  invalid-id  ");
if (sanitized != CorrelationIdValidator.DefaultCorrelationId)
{
    throw new InvalidOperationException("Invalid correlation ids should be normalized.");
}

var valid = CorrelationIdValidator.Normalize("corr-123");
if (valid != "corr-123")
{
    throw new InvalidOperationException("Valid correlation ids should be preserved.");
}

Console.WriteLine("All AFW-DLV-0007.3.4.3 structured-logging and correlation scenarios passed.");
Console.WriteLine(JsonSerializer.Serialize(new { correlationId = correlation.CorrelationId, redacted, sanitized, valid }));
