using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Application.Intents;

public sealed record CreatePaymentIntentRequest(
    Guid SourceWalletId,
    RecipientRequest Recipient,
    long AmountMinor,
    string CurrencyCode,
    string Purpose,
    string? Description,
    string IdempotencyKey,
    Guid? ClientReference = null);

public sealed record RecipientRequest(RecipientType Type, string Reference);

public sealed record CreatePaymentIntentResponse(
    Guid IntentId,
    PaymentIntentStatus Status,
    Guid SourceWalletId,
    RecipientResponse Recipient,
    long AmountMinor,
    string CurrencyCode,
    PaymentPurpose Purpose,
    DateTimeOffset ExpiresAt,
    string NextAction);

public sealed record RecipientResponse(RecipientType Type, string DisplayReference);

public sealed class CreatePaymentIntentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentRecipientResolver _recipientResolver;
    private readonly IPaymentWalletReader _walletReader;

    public CreatePaymentIntentHandler(
        IPaymentIntentRepository repository,
        IPaymentRecipientResolver recipientResolver,
        IPaymentWalletReader walletReader)
    {
        _repository = repository;
        _recipientResolver = recipientResolver;
        _walletReader = walletReader;
    }

    public async Task<CreatePaymentIntentResponse> HandleAsync(CreatePaymentIntentRequest request, Guid payerAwid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new InvalidOperationException("IDEMPOTENCY_KEY_REQUIRED");
        }

        var existing = await _repository.GetByIdempotencyKeyAsync(payerAwid, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var incomingHash = BuildPayloadHash(request);
            if (!string.Equals(existing.PayloadHash, incomingHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("IDEMPOTENCY_CONFLICT");
            }

            return ToResponse(existing);
        }

        if (request.AmountMinor <= 0)
        {
            throw new InvalidOperationException("PAYMENT_AMOUNT_INVALID");
        }

        if (string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            throw new InvalidOperationException("PAYMENT_CURRENCY_INVALID");
        }

        var wallet = await _walletReader.GetAsync(request.SourceWalletId, cancellationToken);
        if (wallet is null)
        {
            throw new InvalidOperationException("PAYMENT_SOURCE_WALLET_NOT_FOUND");
        }

        if (wallet.AwidId != payerAwid)
        {
            throw new InvalidOperationException("PAYMENT_SOURCE_WALLET_FORBIDDEN");
        }

        if (wallet.Status != WalletStatus.Active)
        {
            throw new InvalidOperationException("PAYMENT_SOURCE_WALLET_NOT_ACTIVE");
        }

        if (!string.Equals(wallet.Currency, request.CurrencyCode.Trim().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PAYMENT_CURRENCY_INVALID");
        }

        if (request.Recipient.Type == RecipientType.Wallet && request.Recipient.Reference == request.SourceWalletId.ToString())
        {
            throw new InvalidOperationException("PAYMENT_SELF_TRANSFER_NOT_ALLOWED");
        }

        var resolvedRecipient = await _recipientResolver.ResolveAsync(request.Recipient.Type, request.Recipient.Reference, cancellationToken);
        if (resolvedRecipient is null)
        {
            throw new InvalidOperationException("PAYMENT_RECIPIENT_NOT_FOUND");
        }

        var purpose = ParsePurpose(request.Purpose);

        var intent = new PaymentIntent
        {
            PayerAwid = payerAwid,
            SourceWalletId = request.SourceWalletId,
            RecipientType = request.Recipient.Type,
            RecipientReference = request.Recipient.Reference,
            DestinationWalletId = resolvedRecipient.TargetWalletId,
            AmountMinor = request.AmountMinor,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            Purpose = purpose,
            Description = request.Description ?? string.Empty,
            Status = PaymentIntentStatus.Created,
            IdempotencyKey = request.IdempotencyKey,
            ClientReference = request.ClientReference,
            PayloadHash = BuildPayloadHash(request),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        await _repository.AddAsync(intent, cancellationToken);
        return ToResponse(intent);
    }

    private static PaymentPurpose ParsePurpose(string value)
    {
        if (Enum.TryParse<PaymentPurpose>(value, true, out var purpose))
        {
            return purpose;
        }

        return PaymentPurpose.Other;
    }

    private static string BuildPayloadHash(CreatePaymentIntentRequest request)
    {
        var normalized = string.Join("|",
            request.SourceWalletId,
            request.Recipient.Type,
            request.Recipient.Reference,
            request.AmountMinor,
            request.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty,
            request.Purpose ?? string.Empty,
            request.Description ?? string.Empty,
            request.ClientReference?.ToString() ?? string.Empty);

        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized)));
    }

    private static CreatePaymentIntentResponse ToResponse(PaymentIntent intent)
    {
        return new CreatePaymentIntentResponse(
            intent.Id,
            intent.Status,
            intent.SourceWalletId,
            new RecipientResponse(intent.RecipientType, intent.RecipientReference),
            intent.AmountMinor,
            intent.CurrencyCode,
            intent.Purpose,
            intent.ExpiresAt,
            "VALIDATE");
    }
}
