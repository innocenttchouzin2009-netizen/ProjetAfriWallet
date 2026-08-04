using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MobileMoney.Production.Audit;
using MobileMoney.Production.Extensions;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Audit:RetentionDays"] = "2555",
        ["Audit:EnableExport"] = "true",
        ["Audit:ImmutableStorage"] = "true",
        ["Audit:CompressAfterDays"] = "30"
    })
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddMobileMoneyAudit(configuration);

using var provider = services.BuildServiceProvider();
var auditService = provider.GetRequiredService<IAuditService>();

var first = auditService.Record(new AuditRecord
{
    Action = AuditAction.MtnDepositRequested,
    Category = AuditCategory.Configuration,
    Result = AuditResult.Success,
    TransactionId = "tx-001",
    CorrelationId = "corr-001",
    WalletId = "wallet-001",
    PhoneNumber = "+237670000000"
});

var second = auditService.Record(new AuditRecord
{
    Action = AuditAction.MtnDepositCompleted,
    Category = AuditCategory.Configuration,
    Result = AuditResult.Success,
    TransactionId = "tx-001",
    CorrelationId = "corr-001",
    WalletId = "wallet-001",
    PhoneNumber = "+237670000000"
});

var all = auditService.Search(new AuditSearchCriteria { TransactionId = "tx-001" });
var exported = auditService.Export(new AuditExportFilter { ProviderCode = null });
var chainValid = auditService.VerifyChain(first.AuditId);

if (all.Count < 2)
{
    throw new InvalidOperationException("Expected audit records to be persisted.");
}

if (!chainValid)
{
    throw new InvalidOperationException("Audit hash chain validation failed.");
}

Console.WriteLine("All AFW-DLV-0007.3.4.9 audit trail scenarios passed.");
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { recordCount = all.Count, exportedCount = exported.Count, firstHash = first.CurrentAuditHash, secondPreviousHash = second.PreviousAuditHash }));
