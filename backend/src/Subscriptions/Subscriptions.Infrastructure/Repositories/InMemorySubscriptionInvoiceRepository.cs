using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;

namespace Subscriptions.Infrastructure.Repositories;

public sealed class InMemorySubscriptionInvoiceRepository : ISubscriptionInvoiceRepository
{
    private readonly Dictionary<string, SubscriptionInvoice> _invoicesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string SubscriptionId, DateTimeOffset Start, DateTimeOffset End), string> _index = new();

    public SubscriptionInvoice Add(SubscriptionInvoice invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceId))
        {
            invoice.InvoiceId = Guid.NewGuid().ToString("N");
        }

        _invoicesById[invoice.InvoiceId] = invoice;
        var normalizedStart = Normalize(invoice.BillingPeriodStart);
        var normalizedEnd = Normalize(invoice.BillingPeriodEnd);
        _index[(invoice.SubscriptionId, normalizedStart, normalizedEnd)] = invoice.InvoiceId;
        return invoice;
    }

    public SubscriptionInvoice Update(SubscriptionInvoice invoice)
    {
        _invoicesById[invoice.InvoiceId] = invoice;
        var normalizedStart = Normalize(invoice.BillingPeriodStart);
        var normalizedEnd = Normalize(invoice.BillingPeriodEnd);
        _index[(invoice.SubscriptionId, normalizedStart, normalizedEnd)] = invoice.InvoiceId;
        return invoice;
    }

    public SubscriptionInvoice? GetById(string invoiceId)
    {
        return _invoicesById.TryGetValue(invoiceId, out var invoice) ? invoice : null;
    }

    public SubscriptionInvoice? GetBySubscriptionAndPeriod(string subscriptionId, DateTimeOffset start, DateTimeOffset end)
    {
        var normalizedStart = Normalize(start);
        var normalizedEnd = Normalize(end);
        if (_index.TryGetValue((subscriptionId, normalizedStart, normalizedEnd), out var invoiceId))
        {
            return _invoicesById[invoiceId];
        }

        return _invoicesById.Values.FirstOrDefault(invoice =>
            invoice.SubscriptionId.Equals(subscriptionId, StringComparison.OrdinalIgnoreCase) &&
            Normalize(invoice.BillingPeriodStart) == normalizedStart &&
            Normalize(invoice.BillingPeriodEnd) == normalizedEnd);
    }

    private static DateTimeOffset Normalize(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Offset);
    }
}
