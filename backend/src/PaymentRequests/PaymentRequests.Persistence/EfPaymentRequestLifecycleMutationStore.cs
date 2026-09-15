using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestLifecycleMutationStore(PaymentRequestDbContext dbContext)
    : IPaymentRequestLifecycleMutationStore
{
    public async Task AddAsync(
        PaymentRequest request,
        PaymentRequestLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateEvent(request, lifecycleEvent);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.PaymentRequests.Add(PaymentRequestEntityMapper.ToEntity(request));
        dbContext.PaymentRequestEventOutbox.Add(ToOutboxEntity(lifecycleEvent));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "Payment request and lifecycle event could not be persisted atomically.",
                exception);
        }
    }

    public async Task UpdateAsync(
        PaymentRequest request,
        PaymentRequestLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateEvent(request, lifecycleEvent);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.PaymentRequests
            .SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Payment request was not found.");
        }

        PaymentRequestEntityMapper.Apply(entity, request);
        dbContext.PaymentRequestEventOutbox.Add(ToOutboxEntity(lifecycleEvent));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "Payment request lifecycle update and event could not be persisted atomically.",
                exception);
        }
    }

    private static void ValidateEvent(PaymentRequest request, PaymentRequestLifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        if (lifecycleEvent.PaymentRequestId != request.Id)
        {
            throw new InvalidOperationException("Lifecycle event belongs to a different payment request.");
        }

        if (lifecycleEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("Lifecycle event id cannot be empty.", nameof(lifecycleEvent));
        }

        if (lifecycleEvent.Status != request.Status || lifecycleEvent.OccurredAtUtc != request.UpdatedAtUtc)
        {
            throw new InvalidOperationException("Lifecycle event does not match the current payment request state.");
        }
    }

    private static PaymentRequestEventOutboxEntity ToOutboxEntity(PaymentRequestLifecycleEvent lifecycleEvent)
    {
        var envelope = PaymentRequestLifecycleEventFactory.ToEnvelope(lifecycleEvent);
        var occurredAtUtc = envelope.OccurredAtUtc.UtcDateTime;
        return new PaymentRequestEventOutboxEntity
        {
            EventId = envelope.EventId,
            PaymentRequestId = envelope.PaymentRequestId.Value,
            EventType = envelope.EventType,
            PayloadJson = envelope.PayloadJson,
            OccurredAtUtc = occurredAtUtc,
            EnqueuedAtUtc = occurredAtUtc,
            AvailableAtUtc = occurredAtUtc,
            Status = (int)PaymentRequestEventOutboxStatus.Pending,
            AttemptCount = 0
        };
    }
}
