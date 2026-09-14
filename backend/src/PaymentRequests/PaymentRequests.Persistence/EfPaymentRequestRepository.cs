using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestRepository(PaymentRequestDbContext dbContext) : IPaymentRequestRepository
{
    public async Task<PaymentRequest?> GetAsync(
        PaymentRequestId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.PaymentRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
        return entity is null ? null : PaymentRequestEntityMapper.ToDomain(entity);
    }

    public async Task<PaymentRequest?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        var entity = await dbContext.PaymentRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
        return entity is null ? null : PaymentRequestEntityMapper.ToDomain(entity);
    }

    public async Task AddAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.PaymentRequests.Add(PaymentRequestEntityMapper.ToEntity(request));
        dbContext.PaymentRequestIntegrationOutbox.Add(PaymentRequestOutboxFactory.Created(request));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "Payment request creation or transactional outbox write violated a persistence constraint.",
                exception);
        }
    }

    public async Task UpdateAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.PaymentRequests
            .SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Payment request was not found.");
        }

        var previousStatus = (PaymentRequestStatus)entity.Status;
        PaymentRequestEntityMapper.Apply(entity, request);

        if (previousStatus != request.Status)
        {
            dbContext.PaymentRequestIntegrationOutbox.Add(PaymentRequestOutboxFactory.Transition(request));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "Payment request lifecycle update or transactional outbox write violated a persistence constraint.",
                exception);
        }
    }
}
