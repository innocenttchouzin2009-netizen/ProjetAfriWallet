using Subscriptions.Domain.Models;

namespace Subscriptions.Application.Services;

public sealed record CreateSubscriptionInvoiceRequest(
    string SubscriptionId,
    DateTimeOffset BillingPeriodStart,
    DateTimeOffset BillingPeriodEnd,
    string Currency,
    long AmountMinor,
    SubscriptionBillingCycle BillingCycle,
    DateTimeOffset DueAt);

public interface IPaymentIntentGateway
{
    SubscriptionInvoiceAttempt CreatePaymentIntent(string invoiceId, string currency, long amountMinor);
}

public sealed class FakePaymentIntentGateway : IPaymentIntentGateway
{
    public SubscriptionInvoiceAttempt CreatePaymentIntent(string invoiceId, string currency, long amountMinor)
    {
        return new SubscriptionInvoiceAttempt
        {
            Status = SubscriptionInvoiceAttemptStatus.Succeeded,
            GatewayReference = $"fake-{invoiceId}",
            AttemptedAt = DateTimeOffset.UtcNow
        };
    }
}

public sealed class SubscriptionBillingService
{
    private readonly ISubscriptionInvoiceRepository _repository;
    private readonly IPaymentIntentGateway _paymentIntentGateway;

    public SubscriptionBillingService(ISubscriptionInvoiceRepository repository, IPaymentIntentGateway paymentIntentGateway)
    {
        _repository = repository;
        _paymentIntentGateway = paymentIntentGateway;
    }

    public SubscriptionInvoice CreateInvoice(CreateSubscriptionInvoiceRequest request)
    {
        var existing = _repository.GetBySubscriptionAndPeriod(request.SubscriptionId, request.BillingPeriodStart, request.BillingPeriodEnd);
        if (existing is not null)
        {
            return existing;
        }

        var invoice = new SubscriptionInvoice
        {
            SubscriptionId = request.SubscriptionId,
            BillingPeriodStart = request.BillingPeriodStart,
            BillingPeriodEnd = request.BillingPeriodEnd,
            Currency = request.Currency,
            AmountMinor = request.AmountMinor,
            BillingCycle = request.BillingCycle,
            DueAt = request.DueAt,
            Status = SubscriptionInvoiceStatus.Pending
        };

        return _repository.Add(invoice);
    }

    public SubscriptionInvoiceAttempt ProcessPayment(string invoiceId)
    {
        var invoice = _repository.GetById(invoiceId) ?? throw new InvalidOperationException("Invoice not found.");
        if (invoice.Status == SubscriptionInvoiceStatus.Paid)
        {
            return invoice.Attempts.LastOrDefault() ?? new SubscriptionInvoiceAttempt { Status = SubscriptionInvoiceAttemptStatus.Succeeded };
        }

        var attempt = _paymentIntentGateway.CreatePaymentIntent(invoice.InvoiceId, invoice.Currency, invoice.AmountMinor);
        invoice.Attempts.Add(attempt);
        invoice.RetryCount += 1;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        if (attempt.Status == SubscriptionInvoiceAttemptStatus.Succeeded)
        {
            invoice.Status = SubscriptionInvoiceStatus.Paid;
            invoice.PaidAt = DateTimeOffset.UtcNow;
            _repository.Update(invoice);
            return attempt;
        }

        invoice.Status = invoice.RetryCount >= invoice.MaxRetries ? SubscriptionInvoiceStatus.Failed : SubscriptionInvoiceStatus.Pending;
        _repository.Update(invoice);
        return attempt;
    }
}

public interface ISubscriptionInvoiceRepository
{
    SubscriptionInvoice Add(SubscriptionInvoice invoice);
    SubscriptionInvoice Update(SubscriptionInvoice invoice);
    SubscriptionInvoice? GetById(string invoiceId);
    SubscriptionInvoice? GetBySubscriptionAndPeriod(string subscriptionId, DateTimeOffset start, DateTimeOffset end);
}
