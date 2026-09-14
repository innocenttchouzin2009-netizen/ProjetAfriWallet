using System.Globalization;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestDueReader(PaymentRequestDbContext dbContext)
    : IPaymentRequestDueReader
{
    public async Task<IReadOnlyList<PaymentRequest>> ListDueAsync(
        DateTimeOffset asOfUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (asOfUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Due-request evaluation timestamp must be UTC.", nameof(asOfUtc));
        }

        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Due-request limit must be between 1 and 500.");
        }

        var asOfText = asOfUtc.ToString("O", CultureInfo.InvariantCulture);
        var pending = (int)PaymentRequestStatus.Pending;
        var accepted = (int)PaymentRequestStatus.Accepted;

        var entities = await dbContext.PaymentRequests
            .FromSqlInterpolated($$"""
                SELECT *
                FROM PaymentRequests
                WHERE Status IN ({{pending}}, {{accepted}})
                  AND ExpiresAtUtc IS NOT NULL
                  AND ExpiresAtUtc <= {{asOfText}}
                ORDER BY ExpiresAtUtc ASC, Id ASC
                LIMIT {{limit}}
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .Select(PaymentRequestEntityMapper.ToDomain)
            .ToArray();
    }
}
