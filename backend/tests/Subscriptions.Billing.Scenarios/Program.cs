using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var repository = new InMemorySubscriptionInvoiceRepository();
var gateway = new FakePaymentIntentGateway();
var service = new SubscriptionBillingService(repository, gateway);

var invoice = service.CreateInvoice(new CreateSubscriptionInvoiceRequest(
    SubscriptionId: "sub-001",
    BillingPeriodStart: DateTimeOffset.UtcNow.AddDays(-30),
    BillingPeriodEnd: DateTimeOffset.UtcNow,
    Currency: "XOF",
    AmountMinor: 500000,
    BillingCycle: SubscriptionBillingCycle.Monthly,
    DueAt: DateTimeOffset.UtcNow.AddDays(3)));

var duplicate = service.CreateInvoice(new CreateSubscriptionInvoiceRequest(
    SubscriptionId: "sub-001",
    BillingPeriodStart: DateTimeOffset.UtcNow.AddDays(-30),
    BillingPeriodEnd: DateTimeOffset.UtcNow,
    Currency: "XOF",
    AmountMinor: 500000,
    BillingCycle: SubscriptionBillingCycle.Monthly,
    DueAt: DateTimeOffset.UtcNow.AddDays(3)));

if (duplicate.InvoiceId != invoice.InvoiceId)
{
    Console.Error.WriteLine("Invoice idempotence scenario failed.");
    Environment.Exit(1);
}

var attempt = service.ProcessPayment(invoice.InvoiceId);
if (attempt.Status != SubscriptionInvoiceAttemptStatus.Succeeded)
{
    Console.Error.WriteLine("Invoice payment scenario failed.");
    Environment.Exit(1);
}

var retryResult = service.ProcessPayment(invoice.InvoiceId);
if (retryResult.Status != SubscriptionInvoiceAttemptStatus.Succeeded)
{
    Console.Error.WriteLine("Invoice retry scenario failed.");
    Environment.Exit(1);
}

var storedInvoice = repository.GetById(invoice.InvoiceId)!;
if (storedInvoice.Status != SubscriptionInvoiceStatus.Paid)
{
    Console.Error.WriteLine("Invoice status scenario failed.");
    Environment.Exit(1);
}

Console.WriteLine("All AFW-DLV-0006.4 subscription billing scenarios passed.");
