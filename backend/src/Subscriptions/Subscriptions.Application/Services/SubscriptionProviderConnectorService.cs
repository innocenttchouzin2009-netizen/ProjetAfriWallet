using Subscriptions.Domain.Models;

namespace Subscriptions.Application.Services;

public interface ISubscriptionProviderConnector
{
    string ProviderId { get; }
    SubscriptionProviderConnectorCapabilities DiscoverCapabilities();
    SubscriptionProviderConnectorResponse Activate(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null);
    SubscriptionProviderConnectorResponse Renew(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null);
    SubscriptionProviderConnectorResponse Suspend(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null);
    SubscriptionProviderConnectorResponse Resume(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null);
    SubscriptionProviderConnectorResponse Cancel(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null);
    SubscriptionProviderConnectorResponse GetStatus(string subscriptionId);
    SubscriptionProviderConnectorHealth HealthCheck();
}

public sealed class SubscriptionProviderConnectorRegistry
{
    private readonly Dictionary<string, ISubscriptionProviderConnector> _connectors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> _requestLedger = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ISubscriptionProviderConnector connector)
    {
        _connectors[connector.ProviderId] = connector;
    }

    public ISubscriptionProviderConnector? Get(string providerId)
    {
        return _connectors.TryGetValue(providerId, out var connector) ? connector : null;
    }

    public SubscriptionProviderConnectorCapabilities? DiscoverCapabilities(string providerId)
    {
        return Get(providerId)?.DiscoverCapabilities();
    }

    public SubscriptionProviderConnectorResponse Activate(string providerId, string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        var connector = Get(providerId);
        if (connector is null)
        {
            return new SubscriptionProviderConnectorResponse { ProviderId = providerId, Operation = "activate", Status = SubscriptionConnectorResultStatus.Failed };
        }

        var requestId = payload?.TryGetValue("requestId", out var id) == true ? id : Guid.NewGuid().ToString("N");
        var ledgerKey = $"{providerId}:{subscriptionId}";
        if (_requestLedger.TryGetValue(ledgerKey, out var previousRequest) && previousRequest.TryGetValue("requestId", out var existingRequestId) && existingRequestId == requestId)
        {
            return new SubscriptionProviderConnectorResponse
            {
                ProviderId = providerId,
                Operation = "activate",
                Status = SubscriptionConnectorResultStatus.Success,
                CorrelationId = existingRequestId,
                Message = "duplicate request replayed",
                Payload = new Dictionary<string, string> { ["subscriptionId"] = subscriptionId, ["requestId"] = existingRequestId }
            };
        }

        if (_requestLedger.ContainsKey(ledgerKey))
        {
            return new SubscriptionProviderConnectorResponse
            {
                ProviderId = providerId,
                Operation = "activate",
                Status = SubscriptionConnectorResultStatus.Conflict,
                CorrelationId = requestId,
                Message = "conflicting request for active subscription",
                Payload = new Dictionary<string, string> { ["subscriptionId"] = subscriptionId, ["requestId"] = requestId }
            };
        }

        _requestLedger[ledgerKey] = new Dictionary<string, string> { ["requestId"] = requestId };
        return connector.Activate(subscriptionId, payload);
    }

    public SubscriptionProviderConnectorResponse Renew(string providerId, string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return Get(providerId)?.Renew(subscriptionId, payload) ?? new SubscriptionProviderConnectorResponse { ProviderId = providerId, Operation = "renew", Status = SubscriptionConnectorResultStatus.Failed };
    }

    public SubscriptionProviderConnectorResponse Suspend(string providerId, string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return Get(providerId)?.Suspend(subscriptionId, payload) ?? new SubscriptionProviderConnectorResponse { ProviderId = providerId, Operation = "suspend", Status = SubscriptionConnectorResultStatus.Failed };
    }

    public SubscriptionProviderConnectorResponse Resume(string providerId, string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return Get(providerId)?.Resume(subscriptionId, payload) ?? new SubscriptionProviderConnectorResponse { ProviderId = providerId, Operation = "resume", Status = SubscriptionConnectorResultStatus.Failed };
    }

    public SubscriptionProviderConnectorResponse Cancel(string providerId, string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return Get(providerId)?.Cancel(subscriptionId, payload) ?? new SubscriptionProviderConnectorResponse { ProviderId = providerId, Operation = "cancel", Status = SubscriptionConnectorResultStatus.Failed };
    }

    public SubscriptionProviderConnectorResponse GetStatus(string providerId, string subscriptionId)
    {
        return Get(providerId)?.GetStatus(subscriptionId) ?? new SubscriptionProviderConnectorResponse { ProviderId = providerId, Operation = "status", Status = SubscriptionConnectorResultStatus.Failed };
    }

    public SubscriptionProviderConnectorHealth HealthCheck(string providerId)
    {
        return Get(providerId)?.HealthCheck() ?? new SubscriptionProviderConnectorHealth { ProviderId = providerId, Status = SubscriptionConnectorHealthStatus.Unhealthy, Message = "Connector not found." };
    }
}

public abstract class BaseSandboxConnector : ISubscriptionProviderConnector
{
    public abstract string ProviderId { get; }

    public virtual SubscriptionProviderConnectorCapabilities DiscoverCapabilities() => new()
    {
        SupportsActivation = true,
        SupportsRenewal = true,
        SupportsSuspension = true,
        SupportsResumption = true,
        SupportsCancellation = true,
        SupportsStatusLookup = true
    };

    public virtual SubscriptionProviderConnectorResponse Activate(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return BuildResponse("activate", subscriptionId, payload);
    }

    public virtual SubscriptionProviderConnectorResponse Renew(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return BuildResponse("renew", subscriptionId, payload);
    }

    public virtual SubscriptionProviderConnectorResponse Suspend(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return BuildResponse("suspend", subscriptionId, payload);
    }

    public virtual SubscriptionProviderConnectorResponse Resume(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return BuildResponse("resume", subscriptionId, payload);
    }

    public virtual SubscriptionProviderConnectorResponse Cancel(string subscriptionId, IReadOnlyDictionary<string, string>? payload = null)
    {
        return BuildResponse("cancel", subscriptionId, payload);
    }

    public virtual SubscriptionProviderConnectorResponse GetStatus(string subscriptionId)
    {
        return new SubscriptionProviderConnectorResponse
        {
            ProviderId = ProviderId,
            Operation = "status",
            Status = SubscriptionConnectorResultStatus.Success,
            CorrelationId = subscriptionId,
            Message = "ok",
            Payload = new Dictionary<string, string> { ["status"] = "active" }
        };
    }

    public virtual SubscriptionProviderConnectorHealth HealthCheck() => new()
    {
        ProviderId = ProviderId,
        Status = SubscriptionConnectorHealthStatus.Healthy,
        Message = "sandbox connector healthy"
    };

    protected SubscriptionProviderConnectorResponse BuildResponse(string operation, string subscriptionId, IReadOnlyDictionary<string, string>? payload)
    {
        var requestId = payload?.TryGetValue("requestId", out var id) == true ? id : Guid.NewGuid().ToString("N");
        return new SubscriptionProviderConnectorResponse
        {
            ProviderId = ProviderId,
            Operation = operation,
            Status = SubscriptionConnectorResultStatus.Success,
            CorrelationId = requestId,
            Message = "sandbox success",
            Payload = new Dictionary<string, string> { ["subscriptionId"] = subscriptionId, ["requestId"] = requestId }
        };
    }
}

public sealed class SandboxNetflixConnector : BaseSandboxConnector
{
    public override string ProviderId => "netflix";
}

public sealed class SandboxCanalPlusConnector : BaseSandboxConnector
{
    public override string ProviderId => "canalplus";
}

public sealed class SandboxMyBouquetAfricainConnector : BaseSandboxConnector
{
    public override string ProviderId => "mybouquetafricain";
}

public sealed class SandboxCinafConnector : BaseSandboxConnector
{
    public override string ProviderId => "cinaf";
}
