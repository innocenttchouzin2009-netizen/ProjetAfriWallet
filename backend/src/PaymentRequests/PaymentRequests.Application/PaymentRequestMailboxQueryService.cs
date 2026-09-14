namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestMailboxQueryService(IPaymentRequestMailboxQueryPort queryPort)
{
    public Task<PaymentRequestListPage> ListAsync(
        PaymentRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(query));
        }

        if (!Enum.IsDefined(query.Mailbox))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Mailbox must be Inbox or Outbox.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Page size must be between 1 and 100.");
        }

        if (query.Cursor is not null && string.IsNullOrWhiteSpace(query.Cursor))
        {
            throw new ArgumentException("Cursor must be null or a non-blank opaque value.", nameof(query));
        }

        return queryPort.ListAsync(query, cancellationToken);
    }
}
