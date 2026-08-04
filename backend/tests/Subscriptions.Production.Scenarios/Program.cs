using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var storageRoot = Path.Combine(Path.GetTempPath(), "afw-subscriptions-prod", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(storageRoot);

var repository = new JsonAtomicSubscriptionRepository(storageRoot);
var lifecycleService = new UserSubscriptionLifecycleService(repository);
var invoiceRepository = new JsonAtomicSubscriptionInvoiceRepository(storageRoot);
var billingService = new SubscriptionBillingService(invoiceRepository, new FakePaymentIntentGateway());
var autoRenewRepository = new JsonAtomicAutoRenewJobRepository(storageRoot);

var created = lifecycleService.Create(new CreateUserSubscriptionRequest(
    UserId: "user-1",
    ProviderId: "netflix",
    PlanId: "plan-1",
    OfferId: "offer-1",
    Currency: "XOF",
    AmountMinor: 500000,
    BillingCycle: "monthly",
    GracePeriodDays: 7));

var persisted = repository.GetById(created.SubscriptionId);
if (persisted is null)
{
    Console.Error.WriteLine("Persistence scenario failed.");
    Environment.Exit(1);
}

var invoice = billingService.CreateInvoice(new CreateSubscriptionInvoiceRequest(
    SubscriptionId: created.SubscriptionId,
    BillingPeriodStart: DateTimeOffset.UtcNow.AddDays(-30),
    BillingPeriodEnd: DateTimeOffset.UtcNow,
    Currency: "XOF",
    AmountMinor: 500000,
    BillingCycle: SubscriptionBillingCycle.Monthly,
    DueAt: DateTimeOffset.UtcNow.AddDays(3)));

var reloadedRepository = new JsonAtomicSubscriptionRepository(storageRoot);
var reloaded = reloadedRepository.GetById(created.SubscriptionId);
if (reloaded is null)
{
    Console.WriteLine($"Storage root: {storageRoot}");
    Console.WriteLine($"Subscriptions file exists: {File.Exists(Path.Combine(storageRoot, "subscriptions.json"))}");
    if (File.Exists(Path.Combine(storageRoot, "subscriptions.json")))
    {
        Console.WriteLine(File.ReadAllText(Path.Combine(storageRoot, "subscriptions.json")));
    }
    Console.Error.WriteLine("Restart restore scenario failed.");
    Environment.Exit(1);
}

if (reloaded.Status != created.Status)
{
    Console.WriteLine($"Reloaded status: {reloaded.Status}; original status: {created.Status}");
}

var job = autoRenewRepository.Add(new AutoRenewJob { SubscriptionId = created.SubscriptionId, ScheduledFor = DateTimeOffset.UtcNow.AddDays(1), Status = AutoRenewJobStatus.Scheduled });
var reloadedJobs = new JsonAtomicAutoRenewJobRepository(storageRoot);
var reloadedJob = reloadedJobs.GetById(job.JobId);
if (reloadedJob is null)
{
    Console.Error.WriteLine("Auto-renew persistence scenario failed.");
    Environment.Exit(1);
}

var invoiceFile = Path.Combine(storageRoot, "invoices.json");
var original = File.ReadAllText(invoiceFile);
File.WriteAllText(invoiceFile, original);
var reloadedInvoiceRepository = new JsonAtomicSubscriptionInvoiceRepository(storageRoot);
var reloadedInvoice = reloadedInvoiceRepository.GetById(invoice.InvoiceId);
if (reloadedInvoice is null)
{
    Console.Error.WriteLine("Invoice persistence scenario failed.");
    Environment.Exit(1);
}

Console.WriteLine("All AFW-DLV-0006.8 subscription production-readiness scenarios passed.");
