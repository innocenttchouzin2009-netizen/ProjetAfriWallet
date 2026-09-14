using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Reconciliation.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Reconciliation.Persistence;

public sealed class EfPaymentRequestReconciliationRecordRepository(PaymentRequestReconciliationDbContext dbContext)
    : IPaymentRequestReconciliationRecordRepository
{
    public async Task<PaymentRequestReconciliationRecord?> GetAsync(
        PaymentRequestId requestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.Records.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RequestId == requestId.Value, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task UpsertAsync(
        PaymentRequestReconciliationRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.Records
            .SingleOrDefaultAsync(x => x.RequestId == record.RequestId.Value, cancellationToken);

        if (entity is null)
        {
            dbContext.Records.Add(new PaymentRequestReconciliationEntity
            {
                RequestId = record.RequestId.Value,
                Status = (int)record.Status,
                TransferId = record.TransferId
            });
        }
        else
        {
            entity.Status = (int)record.Status;
            entity.TransferId = record.TransferId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PaymentRequestReconciliationRecord Map(PaymentRequestReconciliationEntity entity)
    {
        if (!Enum.IsDefined(typeof(PaymentRequestReconciliationStatus), entity.Status))
            throw new InvalidOperationException("Stored reconciliation status is invalid.");

        return new PaymentRequestReconciliationRecord(
            PaymentRequestId.From(entity.RequestId),
            (PaymentRequestReconciliationStatus)entity.Status,
            entity.TransferId);
    }
}
