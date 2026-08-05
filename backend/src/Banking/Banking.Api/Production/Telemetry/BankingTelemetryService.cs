using System.Diagnostics.Metrics;

namespace AfriWallet.Banking.Api.Production.Telemetry;

public sealed class BankingTelemetryService
{
    private static readonly Meter Meter = new("AfriWallet.Banking", "1.0.0");
    private static readonly Counter<long> TransfersTotal = Meter.CreateCounter<long>("afw_bank_transfers_total");
    private static readonly Counter<long> TransfersCompleted = Meter.CreateCounter<long>("afw_bank_transfers_completed");
    private static readonly Counter<long> TransfersFailed = Meter.CreateCounter<long>("afw_bank_transfers_failed");
    private static readonly Counter<long> WorkflowsTotal = Meter.CreateCounter<long>("afw_bank_workflows_total");
    private static readonly Histogram<double> WorkflowDurationMs = Meter.CreateHistogram<double>("afw_bank_workflow_duration_ms");
    private static readonly Counter<long> RegistryQueries = Meter.CreateCounter<long>("afw_bank_registry_queries_total");
    private static readonly Counter<long> RoutingTotal = Meter.CreateCounter<long>("afw_bank_routing_total");

    public void TrackTransferCompleted(string? workflowId = null)
    {
        TransfersTotal.Add(1, new KeyValuePair<string, object?>("workflowId", workflowId));
        TransfersCompleted.Add(1, new KeyValuePair<string, object?>("workflowId", workflowId));
    }

    public void TrackTransferFailed(string? workflowId = null)
    {
        TransfersTotal.Add(1, new KeyValuePair<string, object?>("workflowId", workflowId));
        TransfersFailed.Add(1, new KeyValuePair<string, object?>("workflowId", workflowId));
    }

    public void TrackWorkflow(string? workflowId = null)
    {
        WorkflowsTotal.Add(1, new KeyValuePair<string, object?>("workflowId", workflowId));
    }

    public void TrackWorkflowDuration(double durationMs, string? workflowId = null)
    {
        WorkflowDurationMs.Record(durationMs, new KeyValuePair<string, object?>("workflowId", workflowId));
    }

    public void TrackRegistryQuery(string? providerId = null)
    {
        RegistryQueries.Add(1, new KeyValuePair<string, object?>("providerId", providerId));
    }

    public void TrackRouting(string? scheme = null)
    {
        RoutingTotal.Add(1, new KeyValuePair<string, object?>("scheme", scheme));
    }
}
