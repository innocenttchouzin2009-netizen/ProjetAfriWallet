using System.Globalization;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Persistence;

internal static class PaymentRequestEntityMapper
{
    public static PaymentRequestEntity ToEntity(PaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PaymentRequestEntity
        {
            Id = request.Id.Value,
            RequesterWalletId = request.RequesterWalletId.Value,
            PayerReferenceKind = (int)request.PayerReference.Kind,
            PayerReferenceValue = request.PayerReference.Value,
            CurrencyCode = request.Currency.Code,
            AmountMinor = request.AmountMinor,
            CorrelationId = request.CorrelationId,
            CreatedAtUtc = Format(request.CreatedAtUtc),
            ExpiresAtUtc = Format(request.ExpiresAtUtc),
            UpdatedAtUtc = Format(request.UpdatedAtUtc),
            Status = (int)request.Status,
            AcceptedPayerWalletId = request.AcceptedPayerWalletId?.Value,
            AcceptedAtUtc = Format(request.AcceptedAtUtc),
            TransferId = request.TransferId,
            ClosedAtUtc = Format(request.ClosedAtUtc)
        };
    }

    public static void Apply(PaymentRequestEntity entity, PaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(request);

        if (entity.Id != request.Id.Value)
        {
            throw new InvalidOperationException("Payment request persistence identity cannot change.");
        }

        entity.RequesterWalletId = request.RequesterWalletId.Value;
        entity.PayerReferenceKind = (int)request.PayerReference.Kind;
        entity.PayerReferenceValue = request.PayerReference.Value;
        entity.CurrencyCode = request.Currency.Code;
        entity.AmountMinor = request.AmountMinor;
        entity.CorrelationId = request.CorrelationId;
        entity.CreatedAtUtc = Format(request.CreatedAtUtc);
        entity.ExpiresAtUtc = Format(request.ExpiresAtUtc);
        entity.UpdatedAtUtc = Format(request.UpdatedAtUtc);
        entity.Status = (int)request.Status;
        entity.AcceptedPayerWalletId = request.AcceptedPayerWalletId?.Value;
        entity.AcceptedAtUtc = Format(request.AcceptedAtUtc);
        entity.TransferId = request.TransferId;
        entity.ClosedAtUtc = Format(request.ClosedAtUtc);
    }

    public static PaymentRequest ToDomain(PaymentRequestEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var referenceKind = (RecipientReferenceKind)entity.PayerReferenceKind;
        var reference = referenceKind switch
        {
            RecipientReferenceKind.AfWalId => RecipientReference.FromAfWalId(entity.PayerReferenceValue),
            RecipientReferenceKind.QrToken => RecipientReference.FromQrToken(entity.PayerReferenceValue),
            _ => throw new InvalidOperationException("Persisted payer reference kind is unsupported.")
        };

        return PaymentRequest.Restore(
            PaymentRequestId.From(entity.Id),
            WalletId.From(entity.RequesterWalletId),
            reference,
            Currency.Create(entity.CurrencyCode),
            entity.AmountMinor,
            entity.CorrelationId,
            ParseRequired(entity.CreatedAtUtc, nameof(entity.CreatedAtUtc)),
            ParseOptional(entity.ExpiresAtUtc),
            ParseRequired(entity.UpdatedAtUtc, nameof(entity.UpdatedAtUtc)),
            (PaymentRequestStatus)entity.Status,
            entity.AcceptedPayerWalletId is null ? null : WalletId.From(entity.AcceptedPayerWalletId.Value),
            ParseOptional(entity.AcceptedAtUtc),
            entity.TransferId,
            ParseOptional(entity.ClosedAtUtc));
    }

    private static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static string? Format(DateTimeOffset? value) => value?.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseRequired(string value, string fieldName)
    {
        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            throw new InvalidOperationException($"Persisted {fieldName} is invalid.");
        }

        return parsed;
    }

    private static DateTimeOffset? ParseOptional(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            throw new InvalidOperationException("Persisted payment request timestamp is invalid.");
        }

        return parsed;
    }
}
