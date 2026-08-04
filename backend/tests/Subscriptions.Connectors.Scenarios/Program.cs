using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;

var registry = new SubscriptionProviderConnectorRegistry();
registry.Register(new SandboxNetflixConnector());
registry.Register(new SandboxCanalPlusConnector());
registry.Register(new SandboxMyBouquetAfricainConnector());
registry.Register(new SandboxCinafConnector());

var capabilities = registry.DiscoverCapabilities("netflix");
if (capabilities is null || !capabilities.SupportsActivation || !capabilities.SupportsRenewal)
{
    Console.Error.WriteLine("Connector capability scenario failed.");
    Environment.Exit(1);
}

var firstApplyResponse = registry.Activate("netflix", "sub-1", new Dictionary<string, string> { ["requestId"] = "req-1" });
if (firstApplyResponse.Status != SubscriptionConnectorResultStatus.Success)
{
    Console.Error.WriteLine("Connector activation scenario failed.");
    Environment.Exit(1);
}

var duplicateApplyResponse = registry.Activate("netflix", "sub-1", new Dictionary<string, string> { ["requestId"] = "req-1" });
if (duplicateApplyResponse.Status != SubscriptionConnectorResultStatus.Success || duplicateApplyResponse.CorrelationId != firstApplyResponse.CorrelationId)
{
    Console.Error.WriteLine("Connector idempotency scenario failed.");
    Environment.Exit(1);
}

var conflictingApplyResponse = registry.Activate("netflix", "sub-1", new Dictionary<string, string> { ["requestId"] = "req-2" });
if (conflictingApplyResponse.Status != SubscriptionConnectorResultStatus.Conflict)
{
    Console.Error.WriteLine("Connector conflict scenario failed.");
    Environment.Exit(1);
}

var health = registry.HealthCheck("netflix");
if (health.Status != SubscriptionConnectorHealthStatus.Healthy)
{
    Console.Error.WriteLine("Connector health scenario failed.");
    Environment.Exit(1);
}

Console.WriteLine("All AFW-DLV-0006.6 provider connector scenarios passed.");
