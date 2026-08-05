namespace PaymentGateway.Api.Domain;

public enum ExecutionStatus
{
    Queued,
    Dispatching,
    Sent,
    Accepted,
    Processing,
    Settled,
    Completed,
    Failed,
    Cancelled,
    RolledBack
}
