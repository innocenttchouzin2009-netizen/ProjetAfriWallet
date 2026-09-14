using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestApplicationService
{
    private readonly IPaymentRequestRepository repository;
    private readonly IPaymentRequestRecipientResolver recipientResolver;
    private readonly PaymentRequestEventDispatcher? eventDispatcher;

    public PaymentRequestApplicationService(
        IPaymentRequestRepository repository,
        IPaymentRequestRecipientResolver recipientResolver)
        : this(repository, recipientResolver, null)
    {
    }

    public PaymentRequestApplicationService(
        IPaymentRequestRepository repository,
        IPaymentRequestRecipientResolver recipientResolver,
        PaymentRequestEventDispatcher? eventDispatcher)
    {
        this.repository = repository;
        this.recipientResolver = recipientResolver;
        this.eventDispatcher = eventDispatcher;
    }

    public async Task<CreatePaymentRequestResult> CreateAsync(
        CreatePaymentRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.PayerReference);
        ArgumentNullException.ThrowIfNull(command.Currency);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(command));
        }

        var existing = await repository.FindByCorrelationIdAsync(command.CorrelationId, cancellationToken);
        if (existing is not null)
        {
            EnsureEquivalent(existing, command);
            return CreatePaymentRequestResult.Existing(existing);
        }

        var payerWalletId = await recipientResolver.ResolveAsync(
            command.PayerReference,
            command.Currency,
            cancellationToken);

        if (payerWalletId is null)
        {
            return CreatePaymentRequestResult.RecipientNotFound();
        }

        if (payerWalletId.Value == command.RequesterWalletId)
        {
            return CreatePaymentRequestResult.SelfRequestNotAllowed();
        }

        var request = PaymentRequest.Create(
            command.RequesterWalletId,
            command.PayerReference,
            command.Currency,
            command.AmountMinor,
            command.CorrelationId,
            command.CreatedAtUtc,
            command.ExpiresAtUtc);

        await repository.AddAsync(request, cancellationToken);
        await PublishAsync(request, PaymentRequestEventKind.Created, request.CreatedAtUtc, null, cancellationToken);
        return CreatePaymentRequestResult.Created(request);
    }

    public async Task<PaymentRequestSnapshot?> GetAsync(
        PaymentRequestId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = await repository.GetAsync(id, cancellationToken);
        return request is null ? null : PaymentRequestMappings.ToSnapshot(request);
    }

    private Task PublishAsync(
        PaymentRequest request,
        PaymentRequestEventKind kind,
        DateTimeOffset occurredAtUtc,
        Guid? transferId,
        CancellationToken cancellationToken) =>
        eventDispatcher is null
            ? Task.CompletedTask
            : eventDispatcher.PublishAsync(
                PaymentRequestEvent.New(request.Id.Value, kind, occurredAtUtc, transferId),
                cancellationToken);

    private static void EnsureEquivalent(PaymentRequest existing, CreatePaymentRequestCommand command)
    {
        if (existing.RequesterWalletId != command.RequesterWalletId ||
            existing.PayerReference != command.PayerReference ||
            existing.Currency != command.Currency ||
            existing.AmountMinor != command.AmountMinor ||
            existing.ExpiresAtUtc != command.ExpiresAtUtc)
        {
            throw new InvalidOperationException("Correlation id is already associated with a different payment request.");
        }
    }
}
