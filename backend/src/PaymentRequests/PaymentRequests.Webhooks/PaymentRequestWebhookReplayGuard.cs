namespace AfriWallet.PaymentRequests.Webhooks;

public interface IPaymentRequestWebhookReplayGuard
{
    bool TryAccept(Guid eventId, DateTimeOffset nowUtc, DateTimeOffset retainUntilUtc);
}

public sealed class InMemoryPaymentRequestWebhookReplayGuard : IPaymentRequestWebhookReplayGuard
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, DateTimeOffset> accepted = new();
    private readonly int maximumEntries;

    public InMemoryPaymentRequestWebhookReplayGuard(int maximumEntries = 10_000)
    {
        if (maximumEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));

        this.maximumEntries = maximumEntries;
    }

    public bool TryAccept(Guid eventId, DateTimeOffset nowUtc, DateTimeOffset retainUntilUtc)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Webhook event id cannot be empty.", nameof(eventId));
        EnsureUtc(nowUtc, nameof(nowUtc));
        EnsureUtc(retainUntilUtc, nameof(retainUntilUtc));
        if (retainUntilUtc <= nowUtc)
            throw new ArgumentException("Replay retention must extend into the future.", nameof(retainUntilUtc));

        lock (gate)
        {
            foreach (var expired in accepted.Where(pair => pair.Value <= nowUtc).Select(pair => pair.Key).ToArray())
                accepted.Remove(expired);

            if (accepted.ContainsKey(eventId))
                return false;

            if (accepted.Count >= maximumEntries)
                throw new InvalidOperationException("Webhook replay guard capacity has been reached.");

            accepted[eventId] = retainUntilUtc;
            return true;
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
