using Operations.Contracts;
using Operations.Domain;
using Operations.Infrastructure;

namespace Operations.Application;

public sealed class OperationsPortalService
{
    private readonly InMemoryOperationsStore _store;
    private readonly OperationsAuthorizationService _authorizationService;

    public OperationsPortalService(InMemoryOperationsStore store, OperationsAuthorizationService authorizationService)
    {
        _store = store;
        _authorizationService = authorizationService;
    }

    public OperationsDashboardResponse GetDashboard(OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.ViewDashboard);
        _store.Metrics["afw_operations_dashboard_views_total"] += 1;
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.DashboardViewed, context, "dashboard", "global"));

        return new OperationsDashboardResponse
        {
            OpenSupportCases = 3,
            OpenRiskAlerts = 2,
            OpenComplianceCases = 1,
            ActiveWallets = _store.Wallets.Count(x => x.Status == "ACTIVE"),
            ProcessingPayments = 4,
            ServiceIncidents = 1,
            HealthSummary = new Dictionary<string, string>(_store.Services, StringComparer.OrdinalIgnoreCase),
            Metrics = new Dictionary<string, long>(_store.Metrics, StringComparer.OrdinalIgnoreCase)
        };
    }

    public OperationsSearchResponse Search(OperationsSearchRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.SearchGlobal);
        _store.Metrics["afw_operations_searches_total"] += 1;
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.SearchExecuted, context, "search", request.Query ?? string.Empty));

        var query = (request.Query ?? string.Empty).Trim();
        var items = new List<OperationsSearchItem>();

        foreach (var user in _store.Users.Where(x => Matches(query, x.Awid, x.FullName, x.Country)))
        {
            items.Add(new OperationsSearchItem
            {
                Type = "USER",
                Id = user.Awid,
                Title = user.FullName,
                Summary = $"Country: {user.Country}, Risk: {user.RiskLabel}",
                MaskedData = user.EmailMasked
            });
        }

        foreach (var tx in _store.Transactions.Where(x => Matches(query, x.TransactionId, x.Awid, x.WalletId, x.CardId ?? string.Empty)))
        {
            items.Add(new OperationsSearchItem
            {
                Type = "TRANSACTION",
                Id = tx.TransactionId,
                Title = $"Transaction {tx.TransactionId}",
                Summary = $"{tx.Amount} {tx.Currency} / {tx.Status}",
                MaskedData = MaskTransaction(tx)
            });
        }

        foreach (var wallet in _store.Wallets.Where(x => Matches(query, x.WalletId, x.Awid)))
        {
            items.Add(new OperationsSearchItem
            {
                Type = "WALLET",
                Id = wallet.WalletId,
                Title = $"Wallet {wallet.WalletId}",
                Summary = wallet.Status,
                MaskedData = wallet.Reason
            });
        }

        foreach (var card in _store.Cards.Where(x => Matches(query, x.CardId, x.Awid)))
        {
            items.Add(new OperationsSearchItem
            {
                Type = "CARD",
                Id = card.CardId,
                Title = $"Card {card.CardId}",
                Summary = card.Status,
                MaskedData = card.Reason
            });
        }

        foreach (var assignment in _store.SupportCaseAssignments.Where(x => string.IsNullOrWhiteSpace(query) || x.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(new OperationsSearchItem
            {
                Type = "SUPPORT_CASE",
                Id = assignment,
                Title = assignment,
                Summary = "Support case link",
                MaskedData = "[MASKED]"
            });
        }

        return new OperationsSearchResponse { Items = items };
    }

    public OperationsUserResponse GetUser(string awid, OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.ViewUser);
        var user = _store.Users.Single(x => x.Awid == awid);
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.UserViewed, context, "user", awid));
        return new OperationsUserResponse
        {
            Awid = user.Awid,
            FullName = user.FullName,
            EmailMasked = user.EmailMasked,
            Country = user.Country,
            Status = user.Status,
            RiskLabel = user.RiskLabel,
            SupportSummary = user.SupportSummary
        };
    }

    public OperationsTransactionResponse GetTransaction(string transactionId, OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.ViewTransaction);
        var tx = _store.Transactions.Single(x => x.TransactionId == transactionId);
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.TransactionViewed, context, "transaction", transactionId));
        return new OperationsTransactionResponse
        {
            TransactionId = tx.TransactionId,
            Awid = tx.Awid,
            WalletId = tx.WalletId,
            CardId = tx.CardId,
            Status = tx.Status,
            Amount = tx.Amount,
            Currency = tx.Currency,
            Timeline = tx.Timeline.ToList(),
            MaskedData = MaskTransaction(tx)
        };
    }

    public OperationsHealthResponse GetHealth(OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.ViewHealth);
        return new OperationsHealthResponse
        {
            Services = new Dictionary<string, string>(_store.Services, StringComparer.OrdinalIgnoreCase),
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public OperationsAuditResponse GetAudit(OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.ViewAudit);
        _store.Metrics["afw_operations_audit_views_total"] += 1;
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.AuditViewed, context, "audit", "global"));
        return new OperationsAuditResponse
        {
            Events = _store.AuditEntries.Select(x => $"{x.CreatedAtUtc:O} {x.EventType} {x.SubjectType}:{x.SubjectId} {x.Justification}").ToList()
        };
    }

    public OperationsWalletRecord SuspendWallet(string walletId, SuspendWalletRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.SuspendWallet, requireMfa: true, requireDeviceTrust: true);
        ValidateCriticalAction(request.ActorId, request.Justification, request.ConfirmedBy);
        var wallet = _store.Wallets.Single(x => x.WalletId == walletId);
        wallet.Status = "SUSPENDED";
        wallet.Reason = request.Justification;
        wallet.SuspendedAtUtc = DateTimeOffset.UtcNow;
        _store.Metrics["afw_operations_wallet_suspensions_total"] += 1;
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.WalletSuspended, context, "wallet", walletId, request.Justification));
        return wallet;
    }

    public OperationsCardRecord FreezeCard(string cardId, FreezeCardRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.FreezeCard, requireMfa: true, requireDeviceTrust: true);
        ValidateCriticalAction(request.ActorId, request.Justification, request.ConfirmedBy);
        var card = _store.Cards.Single(x => x.CardId == cardId);
        card.Status = "FROZEN";
        card.Reason = request.Justification;
        card.FrozenAtUtc = DateTimeOffset.UtcNow;
        _store.Metrics["afw_operations_card_freezes_total"] += 1;
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.CardFrozen, context, "card", cardId, request.Justification));
        return card;
    }

    public string AssignCase(string caseId, AssignCaseRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.AssignCase, requireMfa: true);
        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            throw new InvalidOperationException("Justification is required.");
        }

        var entry = $"{caseId} -> {request.Assignee}";
        _store.SupportCaseAssignments.Add(entry);
        _store.Metrics["afw_operations_case_assignments_total"] += 1;
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.CaseAssigned, context, "support-case", caseId, request.Justification));
        return entry;
    }

    public OperationsTransactionResponse RetryTransaction(string transactionId, RetryTransactionRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureAllowed(context, OperationsAction.RetryTransaction, requireMfa: true, requireDeviceTrust: true);
        ValidateCriticalAction(request.ActorId, request.Justification, request.ConfirmedBy);
        var tx = _store.Transactions.Single(x => x.TransactionId == transactionId);
        tx.Timeline.Add("RETRY_REQUESTED");
        _store.Metrics["afw_operations_transaction_retries_total"] += 1;
        _store.AuditEntries.Add(BuildAudit(OperationsAuditEventType.TransactionRetried, context, "transaction", transactionId, request.Justification));
        return new OperationsTransactionResponse
        {
            TransactionId = tx.TransactionId,
            Awid = tx.Awid,
            WalletId = tx.WalletId,
            CardId = tx.CardId,
            Status = tx.Status,
            Amount = tx.Amount,
            Currency = tx.Currency,
            Timeline = tx.Timeline.ToList(),
            MaskedData = MaskTransaction(tx)
        };
    }

    private static bool Matches(string query, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return values.Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static string MaskTransaction(OperationsTransactionRecord tx)
    {
        var raw = $"awid={tx.Awid};wallet={tx.WalletId};card={tx.CardId ?? string.Empty}";
        return OperationsParsingExtensions.MaskDigits(raw.Replace(tx.Awid, "[REDACTED_AWID]", StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateCriticalAction(string actorId, string justification, string confirmedBy)
    {
        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(justification) || string.IsNullOrWhiteSpace(confirmedBy))
        {
            throw new InvalidOperationException("Critical action requires actor, justification, and confirmation.");
        }
    }

    private static OperationsAuditEntry BuildAudit(string eventType, OperationsContextRequest context, string subjectType, string subjectId, string justification = "")
    {
        return new OperationsAuditEntry
        {
            EventType = eventType,
            ActorRole = context.Role,
            ActorId = context.ActorId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Justification = justification
        };
    }
}
