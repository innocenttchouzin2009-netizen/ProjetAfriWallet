using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var jobRepository = new InMemoryAutoRenewJobRepository();
var invoiceRepository = new InMemorySubscriptionInvoiceRepository();
var lifecycleRepository = new InMemoryUserSubscriptionRepository();
var lifecycleService = new UserSubscriptionLifecycleService(lifecycleRepository);
var billingService = new SubscriptionBillingService(invoiceRepository, new FakePaymentIntentGateway());
var notificationGateway = new FakeNotificationGateway();
var service = new AutoRenewService(jobRepository, billingService, lifecycleService, notificationGateway);

var subscription = lifecycleService.Create(new CreateUserSubscriptionRequest(
    UserId: "user-2",
    ProviderId: "netflix",
    PlanId: "plan-netflix",
    OfferId: "offer-netflix",
    Currency: "XOF",
    AmountMinor: 500000,
    BillingCycle: "monthly",
    GracePeriodDays: 7));

lifecycleService.Activate(subscription.SubscriptionId);
var renewalDate = DateTimeOffset.UtcNow.AddDays(-1);
var job = service.ScheduleRenewal(new ScheduleAutoRenewRequest(subscription.SubscriptionId, renewalDate));

var processed = service.ProcessDueRenewals(DateTimeOffset.UtcNow);
if (processed.Count != 1 || processed[0].JobId != job.JobId)
{
    Console.Error.WriteLine("Auto-renew scheduling scenario failed.");
    Environment.Exit(1);
}

var invoice = invoiceRepository.GetBySubscriptionAndPeriod(subscription.SubscriptionId, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);
if (invoice is null)
{
    Console.Error.WriteLine("Auto-renew invoice scenario failed.");
    Environment.Exit(1);
}

Console.WriteLine("All AFW-DLV-0006.5 subscription auto-renew scenarios passed.");
