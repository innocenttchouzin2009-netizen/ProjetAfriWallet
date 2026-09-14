using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestExpiryService(
    IPaymentRequestDueReader dueReader,
    IPaymentRequestRepository repository)
{
    public async Task<ExpireDuePaymentRequestsResult> ExpireDueAsync(
        ExpireDuePaymentRequestsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.AsOfUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Expiration evaluation timestamp must be UTC.", nameof(command));
        }

        if (command.BatchSize is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Batch size must be between 1 and 500.");
        }

        var due = await dueReader.ListDueAsync(command.AsOfUtc, command.BatchSize, cancellationToken);
        ArgumentNullException.ThrowIfNull(due);

        var expired = 0;
        var skipped = 0;

        foreach (var request in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request is null)
            {
                throw new InvalidOperationException("Due-request reader returned a null payment request.");
            }

            if (request.Status is not (PaymentRequestStatus.Pending or PaymentRequestStatus.Accepted) ||
                request.ExpiresAtUtc is null ||
                request.ExpiresAtUtc.Value > command.AsOfUtc)
            {
                skipped++;
                continue;
            }

            request.Expire(command.AsOfUtc);
            await repository.UpdateAsync(request, cancellationToken);
            expired++;
        }

        return new ExpireDuePaymentRequestsResult(due.Count, expired, skipped);
    }
}
