using Operations.Domain;

namespace Operations.Infrastructure;

public sealed class InMemoryOperationsStore
{
    public List<OperationsUserRecord> Users { get; } = new();
    public List<OperationsTransactionRecord> Transactions { get; } = new();
    public List<OperationsWalletRecord> Wallets { get; } = new();
    public List<OperationsCardRecord> Cards { get; } = new();
    public List<string> SupportCaseAssignments { get; } = new();
    public List<OperationsAuditEntry> AuditEntries { get; } = new();
    public Dictionary<string, string> Services { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["identity"] = "healthy",
        ["wallet"] = "healthy",
        ["payments"] = "healthy",
        ["cards"] = "healthy",
        ["merchant"] = "healthy",
        ["subscriptions"] = "healthy",
        ["support"] = "healthy",
        ["risk"] = "healthy",
        ["notifications"] = "healthy"
    };
    public Dictionary<string, long> Metrics { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["afw_operations_dashboard_views_total"] = 0,
        ["afw_operations_searches_total"] = 0,
        ["afw_operations_wallet_suspensions_total"] = 0,
        ["afw_operations_card_freezes_total"] = 0,
        ["afw_operations_case_assignments_total"] = 0,
        ["afw_operations_transaction_retries_total"] = 0,
        ["afw_operations_audit_views_total"] = 0
    };

    public InMemoryOperationsStore()
    {
        Users.Add(new OperationsUserRecord
        {
            Awid = "aw-1001",
            FullName = "Awa Traore",
            EmailMasked = MaskEmail("awa.traore@example.com"),
            Country = "CI",
            RiskLabel = "LOW",
            SupportSummary = "1 open case"
        });

        Transactions.Add(new OperationsTransactionRecord
        {
            TransactionId = "tx-9001",
            Awid = "aw-1001",
            WalletId = "wal-1001",
            CardId = "card-2001",
            Amount = 25000,
            Currency = "XOF",
            Timeline = new List<string> { "INITIATED", "AUTHORIZED", "SETTLED" }
        });

        Wallets.Add(new OperationsWalletRecord
        {
            WalletId = "wal-1001",
            Awid = "aw-1001",
            Status = "ACTIVE"
        });

        Cards.Add(new OperationsCardRecord
        {
            CardId = "card-2001",
            Awid = "aw-1001",
            Status = "ACTIVE"
        });
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "[REDACTED_EMAIL]";
        }

        return email[..1] + "***" + email[atIndex..];
    }
}
