using System.Collections.Concurrent;
using AfriWallet.PaymentPlatform.MobileMoney.Domain;

namespace AfriWallet.PaymentPlatform.MobileMoney.Application;

public sealed class MobileMoneyGateway
{
    private readonly IMobileMoneyProviderRegistry _registry;
    private readonly ConcurrentDictionary<Guid, MobileMoneyPayment> _payments = new();
    private readonly ConcurrentDictionary<string, Guid> _idempotency = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Guid> _providerReferences = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<MobileMoneyAuditEvent> _audit = new();
    private readonly ConcurrentQueue<MobileMoneyTelemetryEvent> _telemetry = new();

    public MobileMoneyGateway(IMobileMoneyProviderRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyCollection<MobileMoneyAuditEvent> AuditEvents
        => _audit.ToArray();

    public IReadOnlyCollection<MobileMoneyTelemetryEvent> TelemetryEvents
        => _telemetry.ToArray();

    public async Task<MobileMoneyPayment> InitiateAsync(
        InitiateMobileMoneyRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        if (_idempotency.TryGetValue(request.IdempotencyKey, out var existingId))
            return Get(existingId);

        var provider = _registry.GetRequired(request.ProviderCode);
        ValidateProviderCompatibility(provider.Definition, request);

        var now = DateTimeOffset.UtcNow;
        var payment = new MobileMoneyPayment
        {
            Id = Guid.NewGuid(),
            PaymentIntentId = request.PaymentIntentId,
            ProviderCode = provider.Definition.Code,
            Country = request.Country.ToUpperInvariant(),
            Currency = request.Currency.ToUpperInvariant(),
            Msisdn = request.Msisdn,
            Amount = request.Amount,
            IdempotencyKey = request.IdempotencyKey,
            Status = MobileMoneyPaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        _payments[payment.Id] = payment;

        if (!_idempotency.TryAdd(request.IdempotencyKey, payment.Id))
        {
            _payments.TryRemove(payment.Id, out _);
            return Get(_idempotency[request.IdempotencyKey]);
        }

        Audit("mobile_money.payment.created", payment);

        try
        {
            payment.Status = MobileMoneyPaymentStatus.Processing;

            var result = await provider.InitiateAsync(
                new ProviderPaymentRequest(
                    payment.Id,
                    payment.PaymentIntentId,
                    payment.Country,
                    payment.Currency,
                    payment.Msisdn,
                    payment.Amount,
                    payment.IdempotencyKey),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(result.ProviderReference))
            {
                throw new MobileMoneyException(
                    "provider_reference_missing",
                    "The provider did not return a transaction reference.");
            }

            payment.ProviderReference = result.ProviderReference;
            payment.Status = result.Status;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            _providerReferences[ProviderReferenceKey(payment.ProviderCode, result.ProviderReference)] = payment.Id;

            Audit("mobile_money.payment.initiated", payment);
            Telemetry("mobile_money.initiation", payment);

            return payment;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            payment.Status = MobileMoneyPaymentStatus.Cancelled;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            Audit("mobile_money.payment.cancelled", payment);
            Telemetry("mobile_money.initiation", payment);
            throw;
        }
        catch
        {
            payment.Status = MobileMoneyPaymentStatus.Failed;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            Audit("mobile_money.payment.failed", payment);
            Telemetry("mobile_money.initiation", payment);
            throw;
        }
    }

    public MobileMoneyPayment Get(Guid id)
    {
        if (_payments.TryGetValue(id, out var payment))
            return payment;

        throw new MobileMoneyException(
            "payment_not_found",
            $"Mobile Money payment '{id}' was not found.");
    }

    public async Task<MobileMoneyPayment> RefreshStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = Get(id);

        if (string.IsNullOrWhiteSpace(payment.ProviderReference))
        {
            throw new MobileMoneyException(
                "provider_reference_missing",
                "Provider reference is not available.");
        }

        var provider = _registry.GetRequired(payment.ProviderCode);
        var result = await provider.GetStatusAsync(payment.ProviderReference, cancellationToken);

        payment.Status = result.Status;
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        Audit("mobile_money.status.refreshed", payment);
        Telemetry("mobile_money.status", payment);

        return payment;
    }

    public async Task<MobileMoneyPayment> ProcessCallbackAsync(
        MobileMoneyCallback callback,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callback.ProviderCode) ||
            string.IsNullOrWhiteSpace(callback.ProviderReference))
        {
            throw new MobileMoneyException(
                "invalid_callback",
                "Provider code and provider reference are required.");
        }

        var provider = _registry.GetRequired(callback.ProviderCode);
        var status = await provider.ProcessCallbackAsync(callback, cancellationToken);
        var key = ProviderReferenceKey(provider.Definition.Code, callback.ProviderReference);

        if (!_providerReferences.TryGetValue(key, out var paymentId))
        {
            throw new MobileMoneyException(
                "payment_not_found",
                $"No payment is associated with provider reference '{callback.ProviderReference}'.");
        }

        var payment = Get(paymentId);
        payment.Status = status;
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        Audit("mobile_money.callback.processed", payment);
        Telemetry("mobile_money.callback", payment);

        return payment;
    }

    private static void Validate(InitiateMobileMoneyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
        {
            throw new MobileMoneyException(
                "payment_intent_required",
                "PaymentIntentId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ProviderCode))
        {
            throw new MobileMoneyException(
                "provider_required",
                "ProviderCode is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Country))
        {
            throw new MobileMoneyException(
                "country_required",
                "Country is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            throw new MobileMoneyException(
                "currency_required",
                "Currency is required.");
        }

        if (request.Amount <= 0)
        {
            throw new MobileMoneyException(
                "invalid_amount",
                "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Msisdn))
        {
            throw new MobileMoneyException(
                "msisdn_required",
                "MSISDN is required.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new MobileMoneyException(
                "idempotency_key_required",
                "Idempotency key is required.");
        }
    }

    private static void ValidateProviderCompatibility(
        MobileMoneyProvider provider,
        InitiateMobileMoneyRequest request)
    {
        if (!provider.Enabled)
        {
            throw new MobileMoneyException(
                "provider_disabled",
                $"Provider '{provider.Code}' is disabled.");
        }

        if (!provider.Countries.Contains(request.Country.ToUpperInvariant()))
        {
            throw new MobileMoneyException(
                "country_not_supported",
                $"Provider '{provider.Code}' does not support country '{request.Country}'.");
        }

        if (!provider.Currencies.Contains(request.Currency.ToUpperInvariant()))
        {
            throw new MobileMoneyException(
                "currency_not_supported",
                $"Provider '{provider.Code}' does not support currency '{request.Currency}'.");
        }
    }

    private static string ProviderReferenceKey(string providerCode, string providerReference)
        => $"{providerCode}:{providerReference}";

    private void Audit(string name, MobileMoneyPayment payment)
    {
        _audit.Enqueue(new MobileMoneyAuditEvent(
            name,
            payment.Id,
            payment.ProviderCode,
            DateTimeOffset.UtcNow));
    }

    private void Telemetry(string metric, MobileMoneyPayment payment)
    {
        _telemetry.Enqueue(new MobileMoneyTelemetryEvent(
            metric,
            payment.ProviderCode,
            payment.Status.ToString(),
            DateTimeOffset.UtcNow));
    }
}