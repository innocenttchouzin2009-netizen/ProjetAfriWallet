using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.Logging;

public sealed class StructuredOperationLogger
{
    private readonly ILogger<StructuredOperationLogger> _logger;

    public StructuredOperationLogger(ILogger<StructuredOperationLogger>? logger = null)
    {
        _logger = logger ?? NullLogger<StructuredOperationLogger>.Instance;
    }

    public IDisposable BeginScope(MobileMoneyLoggingScope scope)
    {
        return _logger.BeginScope(scope.ToDictionary()) ?? NullScope.Instance;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    public void LogRequestStarted(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "request-started");
    }

    public void LogRequestAccepted(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "request-accepted");
    }

    public void LogRequestRejected(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "request-rejected");
    }

    public void LogRequestRetried(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "request-retried");
    }

    public void LogRequestTimedOut(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber, TimeSpan elapsed)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "request-timed-out", elapsed);
    }

    public void LogStatusRead(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "status-read");
    }

    public void LogCallbackReceived(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "callback-received");
    }

    public void LogCallbackRejected(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "callback-rejected");
    }

    public void LogTransactionCompleted(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber, TimeSpan elapsed)
    {
        LogEvent(eventName, context, amountMinor, currencyCode, phoneNumber, "transaction-completed", elapsed);
    }

    private void LogEvent(string eventName, CorrelationContext context, int amountMinor, string currencyCode, string? phoneNumber, string operationName, TimeSpan? elapsed = null)
    {
        var phoneNumberMasked = MaskPhoneNumber(phoneNumber);
        var durationMs = (int?)(elapsed?.TotalMilliseconds ?? 0);

        _logger.LogInformation(
            "event={EventName} correlationId={CorrelationId} transactionId={TransactionId} providerReference={ProviderReference} providerCode={ProviderCode} operationType={OperationType} amountMinor={AmountMinor} currencyCode={CurrencyCode} phoneNumberMasked={PhoneNumberMasked} durationMs={DurationMs}",
            eventName,
            context.CorrelationId,
            context.TransactionId,
            context.ProviderReference,
            context.ProviderCode,
            context.OperationType,
            amountMinor,
            currencyCode,
            phoneNumberMasked,
            durationMs);
    }

    private static string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return "N/A";
        }

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4)
        {
            return new string('*', Math.Max(0, digits.Length)) + digits;
        }

        return new string('*', digits.Length - 4) + digits[^4..];
    }
}
