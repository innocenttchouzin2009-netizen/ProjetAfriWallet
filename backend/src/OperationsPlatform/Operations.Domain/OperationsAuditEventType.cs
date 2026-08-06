namespace Operations.Domain;

public static class OperationsAuditEventType
{
    public const string DashboardViewed = "OPERATIONS_DASHBOARD_VIEWED";
    public const string SearchExecuted = "OPERATIONS_SEARCH_EXECUTED";
    public const string UserViewed = "OPERATIONS_USER_VIEWED";
    public const string TransactionViewed = "OPERATIONS_TRANSACTION_VIEWED";
    public const string WalletSuspended = "OPERATIONS_WALLET_SUSPENDED";
    public const string CardFrozen = "OPERATIONS_CARD_FROZEN";
    public const string CaseAssigned = "OPERATIONS_CASE_ASSIGNED";
    public const string TransactionRetried = "OPERATIONS_TRANSACTION_RETRIED";
    public const string ServiceHealthViewed = "OPERATIONS_SERVICE_HEALTH_VIEWED";
    public const string AuditViewed = "OPERATIONS_AUDIT_VIEWED";
}
