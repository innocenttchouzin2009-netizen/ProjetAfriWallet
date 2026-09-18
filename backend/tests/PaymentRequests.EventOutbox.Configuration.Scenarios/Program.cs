using AfriWallet.PaymentRequests.Application;
using IdentityService.Api.PaymentRequests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PaymentRequests:EventOutbox:Dispatcher:Enabled"] = "false",
        ["PaymentRequests:EventOutbox:Dispatcher:BatchSize"] = "17",
        ["PaymentRequests:EventOutbox:Dispatcher:PollIntervalMilliseconds"] = "1250",
        ["PaymentRequests:EventOutbox:Dispatcher:MaxAttempts"] = "9",
        ["PaymentRequests:EventOutbox:Dispatcher:LeaseSeconds"] = "45",
        ["PaymentRequests:EventOutbox:Dispatcher:BaseRetryDelaySeconds"] = "7"
    })
    .Build();

var options = PaymentRequestEventDispatchWorkerOptions.FromConfiguration(configuration);
Assert(!options.Enabled, "Configured dispatcher must be disabled.");
Assert(options.BatchSize == 17, "Configured batch size mismatch.");
Assert(options.PollInterval == TimeSpan.FromMilliseconds(1250), "Configured poll interval mismatch.");
Assert(options.MaxAttempts == 9, "Configured max attempts mismatch.");
Assert(options.LeaseDuration == TimeSpan.FromSeconds(45), "Configured lease duration mismatch.");
Assert(options.BaseRetryDelay == TimeSpan.FromSeconds(7), "Configured retry delay mismatch.");

var services = new ServiceCollection();
services.AddPaymentRequests("Data Source=:memory:", configuration);
using var provider = services.BuildServiceProvider();

var registeredWorkerOptions = provider.GetRequiredService<PaymentRequestEventDispatchWorkerOptions>();
var registeredDeliveryOptions = provider.GetRequiredService<PaymentRequestEventDeliveryOptions>();
Assert(registeredWorkerOptions == options, "Composition must register configured worker options.");
Assert(registeredDeliveryOptions.MaxAttempts == 9, "Configured max attempts must flow into outbox processor options.");
Assert(registeredDeliveryOptions.LeaseDuration == TimeSpan.FromSeconds(45), "Configured lease duration must flow into processor options.");
Assert(registeredDeliveryOptions.BaseRetryDelay == TimeSpan.FromSeconds(7), "Configured retry delay must flow into processor options.");

var state = provider.GetRequiredService<PaymentRequestEventOutboxWorkerState>();
state.MarkDisabled();
Assert(state.Snapshot.Status == PaymentRequestEventOutboxWorkerStatus.Disabled, "Disabled operational state must be observable.");

try
{
    PaymentRequestEventDispatchWorkerOptions.FromConfiguration(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PaymentRequests:EventOutbox:Dispatcher:BatchSize"] = "0"
            })
            .Build());
    throw new InvalidOperationException("Expected invalid batch size to be rejected.");
}
catch (InvalidOperationException exception) when (exception.Message.Contains("batch size", StringComparison.OrdinalIgnoreCase))
{
}

Console.WriteLine("AFW-BE-REQUEST-OUTBOX-REINTEGRATION-1 configuration scenarios: PASS");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
