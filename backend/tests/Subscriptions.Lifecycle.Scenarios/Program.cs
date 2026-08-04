using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var repository = new InMemoryUserSubscriptionRepository();
var service = new UserSubscriptionLifecycleService(repository);

var request = new CreateUserSubscriptionRequest(
    UserId: "user-1",
    ProviderId: "netflix",
    PlanId: "plan-netflix",
    OfferId: "offer-netflix",
    Currency: "XOF",
    AmountMinor: 500000,
    BillingCycle: "monthly",
    GracePeriodDays: 7);

var first = service.Create(request);
var duplicate = service.Create(request);

if (first.SubscriptionId != duplicate.SubscriptionId)
{
    Console.Error.WriteLine("Idempotence scenario failed.");
    Environment.Exit(1);
}

service.MarkPendingPayment(first.SubscriptionId);
service.Activate(first.SubscriptionId);
service.Suspend(first.SubscriptionId);
service.Resume(first.SubscriptionId);
service.Renew(first.SubscriptionId);
service.Cancel(first.SubscriptionId);
service.Expire(first.SubscriptionId);

var subscription = repository.GetById(first.SubscriptionId)!;
if (subscription.Status != UserSubscriptionStatus.Expired)
{
    Console.Error.WriteLine("Lifecycle final state scenario failed.");
    Environment.Exit(1);
}

if (subscription.History.Count < 5)
{
    Console.Error.WriteLine("Lifecycle history scenario failed.");
    Environment.Exit(1);
}

Console.WriteLine("All AFW-DLV-0006.3 subscription lifecycle scenarios passed.");
