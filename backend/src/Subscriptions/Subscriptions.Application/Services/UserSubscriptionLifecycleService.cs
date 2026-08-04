using Subscriptions.Domain.Models;

namespace Subscriptions.Application.Services;

public sealed record CreateUserSubscriptionRequest(
    string UserId,
    string ProviderId,
    string PlanId,
    string OfferId,
    string Currency,
    long AmountMinor,
    string BillingCycle,
    int GracePeriodDays);

public sealed class UserSubscriptionLifecycleService
{
    private readonly IUserSubscriptionRepository _repository;

    public UserSubscriptionLifecycleService(IUserSubscriptionRepository repository)
    {
        _repository = repository;
    }

    public UserSubscription Create(CreateUserSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        var existing = _repository.FindByUserAndProvider(request.UserId, request.ProviderId);
        if (existing is not null)
        {
            return existing;
        }

        var subscription = new UserSubscription
        {
            UserId = request.UserId,
            ProviderId = request.ProviderId,
            PlanId = request.PlanId,
            OfferId = request.OfferId,
            Currency = request.Currency,
            AmountMinor = request.AmountMinor,
            BillingCycle = request.BillingCycle,
            GracePeriodDays = request.GracePeriodDays,
            Status = UserSubscriptionStatus.Draft
        };

        subscription.History.Add(new UserSubscriptionStatusChange
        {
            Status = UserSubscriptionStatus.Draft,
            Reason = "created"
        });

        return _repository.Add(subscription);
    }

    public UserSubscription MarkPendingPayment(string subscriptionId)
    {
        return Transition(subscriptionId, UserSubscriptionStatus.PendingPayment, "payment required");
    }

    public UserSubscription Activate(string subscriptionId)
    {
        var subscription = Transition(subscriptionId, UserSubscriptionStatus.Active, "payment confirmed");
        subscription.StartedAt ??= DateTimeOffset.UtcNow;
        subscription.LastPaymentAt = DateTimeOffset.UtcNow;
        subscription.RenewalAt = DateTimeOffset.UtcNow.AddDays(30);
        return subscription;
    }

    public UserSubscription Suspend(string subscriptionId)
    {
        return Transition(subscriptionId, UserSubscriptionStatus.Suspended, "suspended by policy");
    }

    public UserSubscription Resume(string subscriptionId)
    {
        return Transition(subscriptionId, UserSubscriptionStatus.Active, "resumed");
    }

    public UserSubscription Renew(string subscriptionId)
    {
        var subscription = Transition(subscriptionId, UserSubscriptionStatus.Active, "renewed");
        subscription.LastPaymentAt = DateTimeOffset.UtcNow;
        subscription.RenewalAt = DateTimeOffset.UtcNow.AddDays(30);
        return subscription;
    }

    public UserSubscription Cancel(string subscriptionId)
    {
        return Transition(subscriptionId, UserSubscriptionStatus.Cancelled, "cancelled by user");
    }

    public UserSubscription Expire(string subscriptionId)
    {
        var subscription = Transition(subscriptionId, UserSubscriptionStatus.Expired, "expired after cancellation");
        subscription.EndedAt ??= DateTimeOffset.UtcNow;
        return subscription;
    }

    private UserSubscription Transition(string subscriptionId, UserSubscriptionStatus nextStatus, string reason)
    {
        var subscription = _repository.GetById(subscriptionId) ?? throw new InvalidOperationException("Subscription not found.");
        if (subscription.Status == nextStatus)
        {
            return subscription;
        }

        subscription.Status = nextStatus;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        subscription.History.Add(new UserSubscriptionStatusChange
        {
            Status = nextStatus,
            Reason = reason,
            ChangedAt = DateTimeOffset.UtcNow
        });

        return _repository.Update(subscription);
    }
}

public interface IUserSubscriptionRepository
{
    UserSubscription Add(UserSubscription subscription);
    UserSubscription Update(UserSubscription subscription);
    UserSubscription? GetById(string subscriptionId);
    UserSubscription? FindByUserAndProvider(string userId, string providerId);
}
