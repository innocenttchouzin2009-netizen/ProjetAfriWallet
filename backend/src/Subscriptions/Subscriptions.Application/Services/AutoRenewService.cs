using Subscriptions.Domain.Models;

namespace Subscriptions.Application.Services;

public sealed record ScheduleAutoRenewRequest(string SubscriptionId, DateTimeOffset ScheduledFor);

public interface IAutoRenewJobRepository
{
    AutoRenewJob Add(AutoRenewJob job);
    AutoRenewJob Update(AutoRenewJob job);
    AutoRenewJob? GetById(string jobId);
    IReadOnlyList<AutoRenewJob> ListDue(DateTimeOffset asOf);
}

public interface INotificationGateway
{
    void Notify(string subscriptionId, string message);
}

public sealed class FakeNotificationGateway : INotificationGateway
{
    public void Notify(string subscriptionId, string message)
    {
    }
}

public sealed class AutoRenewService
{
    private readonly IAutoRenewJobRepository _jobRepository;
    private readonly SubscriptionBillingService _billingService;
    private readonly UserSubscriptionLifecycleService _lifecycleService;
    private readonly INotificationGateway _notificationGateway;

    public AutoRenewService(
        IAutoRenewJobRepository jobRepository,
        SubscriptionBillingService billingService,
        UserSubscriptionLifecycleService lifecycleService,
        INotificationGateway notificationGateway)
    {
        _jobRepository = jobRepository;
        _billingService = billingService;
        _lifecycleService = lifecycleService;
        _notificationGateway = notificationGateway;
    }

    public AutoRenewJob ScheduleRenewal(ScheduleAutoRenewRequest request)
    {
        var job = new AutoRenewJob
        {
            SubscriptionId = request.SubscriptionId,
            ScheduledFor = request.ScheduledFor,
            Status = AutoRenewJobStatus.Scheduled
        };

        return _jobRepository.Add(job);
    }

    public IReadOnlyList<AutoRenewJob> ProcessDueRenewals(DateTimeOffset asOf)
    {
        var dueJobs = _jobRepository.ListDue(asOf).ToList();
        foreach (var job in dueJobs)
        {
            if (job.Status == AutoRenewJobStatus.Succeeded || job.Status == AutoRenewJobStatus.Cancelled)
            {
                continue;
            }

            job.Status = AutoRenewJobStatus.Processing;
            job.StartedAt = DateTimeOffset.UtcNow;
            job.Attempts.Add(new AutoRenewAttempt
            {
                AttemptedAt = DateTimeOffset.UtcNow,
                Message = "Processing auto-renewal",
                Succeeded = true
            });

            try
            {
                var invoice = _billingService.CreateInvoice(new CreateSubscriptionInvoiceRequest(
                    SubscriptionId: job.SubscriptionId,
                    BillingPeriodStart: asOf.AddDays(-30),
                    BillingPeriodEnd: asOf,
                    Currency: "XOF",
                    AmountMinor: 500000,
                    BillingCycle: SubscriptionBillingCycle.Monthly,
                    DueAt: asOf.AddDays(3)));

                var attempt = _billingService.ProcessPayment(invoice.InvoiceId);
                if (attempt.Status == SubscriptionInvoiceAttemptStatus.Succeeded)
                {
                    job.Status = AutoRenewJobStatus.Succeeded;
                    job.CompletedAt = DateTimeOffset.UtcNow;
                    _lifecycleService.Renew(job.SubscriptionId);
                    _notificationGateway.Notify(job.SubscriptionId, "Auto-renewal succeeded.");
                }
                else
                {
                    job.RetryCount += 1;
                    job.LastError = "payment failed";
                    job.Status = job.RetryCount >= job.MaxRetries ? AutoRenewJobStatus.GracePeriod : AutoRenewJobStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                job.RetryCount += 1;
                job.LastError = ex.Message;
                job.Status = job.RetryCount >= job.MaxRetries ? AutoRenewJobStatus.GracePeriod : AutoRenewJobStatus.Failed;
            }

            _jobRepository.Update(job);
        }

        return dueJobs;
    }
}
