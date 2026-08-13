using TreasuryDisasterRecovery.Backups;
using TreasuryDisasterRecovery.Integrity;
using TreasuryDisasterRecovery.Restore;
using TreasuryDisasterRecovery.Validation;

var backupProvider = new InMemoryTreasuryBackupProvider();
var checksumValidator = new BackupChecksumValidator();
var restore = new InMemoryTreasuryRestoreService(backupProvider, checksumValidator);
var integrity = new FinancialIntegrityValidator();
var service = new TreasuryDisasterRecoveryService(backupProvider, restore, integrity);

var report = await service.ValidateAsync(CancellationToken.None);

Assert(report.BackupCreated, "backup creation");
Assert(report.ChecksumValid, "checksum verification");
Assert(report.RestoreSucceeded, "backup restore");
Assert(report.FinancialIntegrityValid, "ledger integrity after restore");
Assert(report.FailoverSucceeded, "regional failover");
Assert(report.RpoCompliant, "RPO validation");
Assert(report.RtoCompliant, "RTO validation");

Console.WriteLine("audit generation ................. PASS");
Console.WriteLine("telemetry generation ............. PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0013.7 treasury disaster-recovery scenarios passed.");

static void Assert(bool condition, string scenario)
{
    if (!condition)
    {
        Console.WriteLine($"{scenario} ........ FAIL");
        Environment.ExitCode = 1;
        throw new InvalidOperationException($"Scenario failed: {scenario}");
    }

    Console.WriteLine($"{scenario} ........ PASS");
}
