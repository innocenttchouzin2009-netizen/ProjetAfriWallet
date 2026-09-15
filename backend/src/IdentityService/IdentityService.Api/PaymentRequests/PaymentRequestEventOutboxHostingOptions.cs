using Microsoft.Extensions.Configuration;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestEventOutboxHostingOptions(
    bool Enabled,
    int BatchSize,
    TimeSpan PollInterval,
    int MaxAttempts,
    TimeSpan LeaseDuration,
    TimeSpan BaseRetryDelay)
{
    public static PaymentRequestEventOutboxHostingOptions Default { get; } = new(
        Enabled: false,
        BatchSize: 50,
        PollInterval: TimeSpan.FromSeconds(5),
        MaxAttempts: 5,
        LeaseDuration: TimeSpan.FromMinutes(5),
        BaseRetryDelay: TimeSpan.FromSeconds(30));

    public static PaymentRequestEventOutboxHostingOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        const string prefix = "PaymentRequests:EventOutbox:Dispatcher";
        var defaults = Default;
        var options = new PaymentRequestEventOutboxHostingOptions(
            ReadBool(configuration[$"{prefix}:Enabled"], defaults.Enabled),
            ReadInt(configuration[$"{prefix}:BatchSize"], defaults.BatchSize),
            TimeSpan.FromMilliseconds(ReadInt(configuration[$"{prefix}:PollIntervalMilliseconds"], (int)defaults.PollInterval.TotalMilliseconds)),
            ReadInt(configuration[$"{prefix}:MaxAttempts"], defaults.MaxAttempts),
            TimeSpan.FromSeconds(ReadInt(configuration[$"{prefix}:LeaseSeconds"], (int)defaults.LeaseDuration.TotalSeconds)),
            TimeSpan.FromSeconds(ReadInt(configuration[$"{prefix}:BaseRetryDelaySeconds"], (int)defaults.BaseRetryDelay.TotalSeconds)));

        options.Validate();
        return options;
    }

    public void Validate()
    {
        if (BatchSize is <= 0 or > 1000) throw new InvalidOperationException("Event outbox BatchSize must be between 1 and 1000.");
        if (PollInterval < TimeSpan.FromMilliseconds(10)) throw new InvalidOperationException("Event outbox PollInterval must be at least 10 ms.");
        if (MaxAttempts is <= 0 or > 100) throw new InvalidOperationException("Event outbox MaxAttempts must be between 1 and 100.");
        if (LeaseDuration < TimeSpan.FromSeconds(1)) throw new InvalidOperationException("Event outbox LeaseDuration must be at least 1 second.");
        if (BaseRetryDelay < TimeSpan.FromSeconds(1)) throw new InvalidOperationException("Event outbox BaseRetryDelay must be at least 1 second.");
    }

    private static bool ReadBool(string? raw, bool fallback) =>
        string.IsNullOrWhiteSpace(raw) ? fallback :
        bool.TryParse(raw, out var value) ? value :
        throw new InvalidOperationException($"Invalid boolean value '{raw}' in payment request event outbox configuration.");

    private static int ReadInt(string? raw, int fallback) =>
        string.IsNullOrWhiteSpace(raw) ? fallback :
        int.TryParse(raw, out var value) ? value :
        throw new InvalidOperationException($"Invalid integer value '{raw}' in payment request event outbox configuration.");
}
