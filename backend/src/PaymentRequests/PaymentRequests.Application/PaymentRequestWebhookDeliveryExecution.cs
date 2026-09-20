namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestWebhookSignerRegistry
{
    Task<IPaymentRequestWebhookSigner> ResolveAsync(
        Guid destinationId,
        string signingConfigurationId,
        CancellationToken cancellationToken = default);
}

public interface IPaymentRequestWebhookRetryScheduler
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}

public sealed class SystemPaymentRequestWebhookRetryScheduler : IPaymentRequestWebhookRetryScheduler
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
        Task.Delay(delay, cancellationToken);
}

public sealed record PaymentRequestWebhookDestinationExecution(
    Guid DestinationId,
    PaymentRequestWebhookDeliveryOutcome Outcome,
    int Attempts,
    PaymentRequestWebhookDeliveryAttempt LastAttempt);

public sealed record PaymentRequestWebhookExecutionResult(
    PaymentRequestWebhookDeliveryPlanningStatus PlanningStatus,
    IReadOnlyList<PaymentRequestWebhookDestinationExecution> Destinations);

public sealed class PaymentRequestWebhookDeliveryExecutor(
    PaymentRequestWebhookDeliveryPlanner planner,
    IPaymentRequestWebhookSignerRegistry signerRegistry,
    IPaymentRequestWebhookTransport transport,
    IPaymentRequestWebhookRetryScheduler retryScheduler)
{
    public async Task<PaymentRequestWebhookExecutionResult> ExecuteAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        DateTimeOffset firstAttemptAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        cancellationToken.ThrowIfCancellationRequested();

        if (firstAttemptAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Attempt timestamp must be UTC.", nameof(firstAttemptAtUtc));

        var planning = await planner.PlanAsync(paymentRequestEvent, cancellationToken);
        if (planning.Status != PaymentRequestWebhookDeliveryPlanningStatus.Planned || planning.Plan is null)
            return new PaymentRequestWebhookExecutionResult(planning.Status, []);

        var executions = new List<PaymentRequestWebhookDestinationExecution>(planning.Plan.Destinations.Count);

        foreach (var destination in planning.Plan.Destinations)
        {
            var signer = await signerRegistry.ResolveAsync(
                destination.DestinationId,
                destination.SigningConfigurationId,
                cancellationToken);

            var delivery = new PaymentRequestWebhookDeliveryService(signer, transport);
            PaymentRequestWebhookDeliveryAttempt? lastAttempt = null;
            var attemptAtUtc = firstAttemptAtUtc;

            for (var attemptNumber = 1; attemptNumber <= destination.MaxAttempts; attemptNumber++)
            {
                lastAttempt = await delivery.DeliverOnceAsync(
                    paymentRequestEvent,
                    destination.Endpoint,
                    attemptAtUtc,
                    cancellationToken);

                if (lastAttempt.Outcome is PaymentRequestWebhookDeliveryOutcome.Delivered or
                    PaymentRequestWebhookDeliveryOutcome.PermanentFailure)
                {
                    executions.Add(new PaymentRequestWebhookDestinationExecution(
                        destination.DestinationId,
                        lastAttempt.Outcome,
                        attemptNumber,
                        lastAttempt));
                    break;
                }

                if (attemptNumber == destination.MaxAttempts)
                {
                    executions.Add(new PaymentRequestWebhookDestinationExecution(
                        destination.DestinationId,
                        lastAttempt.Outcome,
                        attemptNumber,
                        lastAttempt));
                    break;
                }

                var delay = destination.RetryDelays[attemptNumber - 1];
                await retryScheduler.DelayAsync(delay, cancellationToken);
                attemptAtUtc = attemptAtUtc.Add(delay);
            }
        }

        return new PaymentRequestWebhookExecutionResult(
            PaymentRequestWebhookDeliveryPlanningStatus.Planned,
            executions);
    }
}
