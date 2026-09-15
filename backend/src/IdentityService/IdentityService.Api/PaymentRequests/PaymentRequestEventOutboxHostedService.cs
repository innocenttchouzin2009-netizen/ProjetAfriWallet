using AfriWallet.PaymentRequests.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.PaymentRequests;

public sealed class PaymentRequestEventOutboxHostedService(
    IServiceScopeFactory scopeFactory,
    PaymentRequestEventOutboxHostingOptions options,
    PaymentRequestEventOutboxDispatcherState state,
    TimeProvider timeProvider,
    ILogger<PaymentRequestEventOutboxHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        options.Validate();

        if (!options.Enabled)
        {
            state.MarkDisabled();
            logger.LogInformation("Payment request event outbox dispatcher is disabled by configuration.");
            return;
        }

        state.MarkIdle();
        logger.LogInformation(
            "Payment request event outbox dispatcher started with batch size {BatchSize} and poll interval {PollInterval}.",
            options.BatchSize,
            options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            var cycleStartedAtUtc = timeProvider.GetUtcNow();

            try
            {
                using var scope = scopeFactory.CreateScope();
                if (scope.ServiceProvider.GetService<IPaymentRequestEventDeliveryPort>() is null)
                {
                    const string error = "Payment request event delivery port is not configured.";
                    state.MarkBlocked(cycleStartedAtUtc, error);
                    logger.LogWarning("{Message} Dispatcher will not claim outbox events.", error);
                }
                else
                {
                    state.MarkCycleStarted(cycleStartedAtUtc);
                    var processor = scope.ServiceProvider.GetRequiredService<PaymentRequestEventOutboxProcessor>();
                    var delivered = await processor.ProcessBatchAsync(
                        options.BatchSize,
                        cycleStartedAtUtc,
                        stoppingToken);
                    state.MarkCycleCompleted(timeProvider.GetUtcNow(), delivered);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                state.MarkCycleFailed(timeProvider.GetUtcNow(), exception.Message);
                logger.LogError(exception, "Payment request event outbox dispatch cycle failed.");
            }

            try
            {
                await Task.Delay(options.PollInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Payment request event outbox dispatcher stopped.");
    }
}
