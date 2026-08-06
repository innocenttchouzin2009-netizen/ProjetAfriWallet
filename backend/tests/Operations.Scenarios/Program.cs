using Operations.Application;
using Operations.Contracts;
using Operations.Infrastructure;

var store = new InMemoryOperationsStore();
var service = new OperationsPortalService(store, new OperationsAuthorizationService());

var ctx = new OperationsContextRequest
{
    ActorId = "ops-100",
    Role = "SUPER_ADMIN",
    HasMfa = true,
    HasDeviceTrust = true
};

var dashboard = service.GetDashboard(ctx);
if (dashboard.OpenSupportCases != 3 || dashboard.Metrics["afw_operations_dashboard_views_total"] != 1) throw new Exception("operations dashboard failed");

var search = service.Search(new OperationsSearchRequest { Query = "aw-1001" }, ctx);
if (!search.Items.Any(x => x.Type == "USER") || !search.Items.Any(x => x.Type == "TRANSACTION")) throw new Exception("global search failed");

var user = service.GetUser("aw-1001", ctx);
if (user.Awid != "aw-1001" || !user.EmailMasked.Contains("***")) throw new Exception("user profile lookup failed");

var transaction = service.GetTransaction("tx-9001", ctx);
if (transaction.Timeline.Count != 3 || !transaction.MaskedData.Contains("[REDACTED_AWID]") ) throw new Exception("transaction timeline failed");

var suspendedWallet = service.SuspendWallet("wal-1001", new SuspendWalletRequest
{
    ActorId = "ops-100",
    Justification = "Fraud review",
    ConfirmedBy = "mgr-200"
}, ctx);
if (suspendedWallet.Status != "SUSPENDED") throw new Exception("wallet suspension failed");

var frozenCard = service.FreezeCard("card-2001", new FreezeCardRequest
{
    ActorId = "ops-100",
    Justification = "Chargeback risk",
    ConfirmedBy = "mgr-200"
}, ctx);
if (frozenCard.Status != "FROZEN") throw new Exception("card freeze failed");

var assigned = service.AssignCase("case-001", new AssignCaseRequest
{
    ActorId = "ops-100",
    Assignee = "support-l2",
    Justification = "Escalate support case to L2"
}, new OperationsContextRequest
{
    ActorId = "ops-100",
    Role = "SUPPORT_MANAGER",
    HasMfa = true,
    HasDeviceTrust = false
});
if (!assigned.Contains("support-l2")) throw new Exception("support case assignment failed");

var retried = service.RetryTransaction("tx-9001", new RetryTransactionRequest
{
    ActorId = "ops-100",
    Justification = "Retry after gateway fix",
    ConfirmedBy = "mgr-200"
}, ctx);
if (!retried.Timeline.Contains("RETRY_REQUESTED")) throw new Exception("controlled retry failed");

var audit = service.GetAudit(ctx);
if (!audit.Events.Any(x => x.Contains("OPERATIONS_WALLET_SUSPENDED")) || !audit.Events.Any(x => x.Contains("OPERATIONS_TRANSACTION_RETRIED"))) throw new Exception("audit generation failed");

var health = service.GetHealth(ctx);
if (!health.Services.ContainsKey("identity") || health.Services.Values.Any(x => x == null)) throw new Exception("operations health failed");

var readonlyCtx = new OperationsContextRequest
{
    ActorId = "viewer-1",
    Role = "READ_ONLY",
    HasMfa = false,
    HasDeviceTrust = false
};
var readonlyDashboard = service.GetDashboard(readonlyCtx);
if (readonlyDashboard.OpenSupportCases != 3) throw new Exception("role based access failed");

var maskedSearch = service.Search(new OperationsSearchRequest { Query = "example.com" }, ctx);
if (maskedSearch.Items.Any(x => x.MaskedData.Contains("example.com", StringComparison.OrdinalIgnoreCase))) throw new Exception("sensitive data masking failed");

Console.WriteLine("role-based access .................... PASS");
Console.WriteLine("global search ........................ PASS");
Console.WriteLine("operations dashboard ................. PASS");
Console.WriteLine("user profile lookup .................. PASS");
Console.WriteLine("transaction timeline ................. PASS");
Console.WriteLine("wallet suspension .................... PASS");
Console.WriteLine("card freeze .......................... PASS");
Console.WriteLine("support case assignment .............. PASS");
Console.WriteLine("controlled retry ..................... PASS");
Console.WriteLine("sensitive data masking ............... PASS");
Console.WriteLine("audit generation ..................... PASS");
Console.WriteLine("telemetry generation ................. PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0012.3 operations portal scenarios passed.");
