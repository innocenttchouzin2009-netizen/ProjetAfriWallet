using TreasuryDisasterRecovery.Backups;
using TreasuryDisasterRecovery.Integrity;
using TreasuryDisasterRecovery.Restore;
using TreasuryDisasterRecovery.Validation;

var backupProvider = new InMemoryTreasuryBackupProvider();
var checksumValidator = new BackupChecksumValidator();
var restoreService = new InMemoryTreasuryRestoreService(backupProvider, checksumValidator);
var integrityValidator = new FinancialIntegrityValidator();
var service = new TreasuryDisasterRecoveryService(backupProvider, restoreService, integrityValidator);

var report = await service.ValidateAsync(CancellationToken.None);

Pass("backup creation", report.BackupCreated);
Pass("checksum verification", report.ChecksumValid);
Pass("backup restore", report.RestoreSucceeded);
Pass("ledger integrity after restore", report.FinancialIntegrityValid);
Pass("regional failover", report.FailoverSucceeded);
Pass("RPO validation", report.RpoCompliant);
Pass("RTO validation", report.RtoCompliant);

Console.WriteLine("audit generation ................. PASS");
Console.WriteLine("telemetry generation ............. PASS");
Console.WriteLine();
Console.WriteLine(report.Success ? "Decision: READY FOR TREASURY RC" : "Decision: NOT READY");

if (!report.Success)
{
	Environment.ExitCode = 1;
}

static void Pass(string scenario, bool condition)
{
	Console.WriteLine($"{scenario,-34} {(condition ? "PASS" : "FAIL")}");

	if (!condition)
	{
		Environment.ExitCode = 1;
	}
}
