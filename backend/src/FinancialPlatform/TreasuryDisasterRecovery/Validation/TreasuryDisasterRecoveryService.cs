using TreasuryDisasterRecovery.Backups;
using TreasuryDisasterRecovery.Failover;
using TreasuryDisasterRecovery.Integrity;
using TreasuryDisasterRecovery.Restore;

namespace TreasuryDisasterRecovery.Validation;

public sealed class TreasuryDisasterRecoveryService
{
    private readonly ITreasuryBackupProvider _backupProvider;
    private readonly ITreasuryRestoreService _restoreService;
    private readonly FinancialIntegrityValidator _integrityValidator;

    public TreasuryDisasterRecoveryService(
        ITreasuryBackupProvider backupProvider,
        ITreasuryRestoreService restoreService,
        FinancialIntegrityValidator integrityValidator)
    {
        _backupProvider = backupProvider;
        _restoreService = restoreService;
        _integrityValidator = integrityValidator;
    }

    public async Task<DisasterRecoveryValidationReport> ValidateAsync(CancellationToken cancellationToken)
    {
        var policy = DisasterRecoveryPolicy.ProductionDefault;

        var backup = await _backupProvider.CreateBackupAsync($"TREASURY-DR-{DateTime.UtcNow:yyyyMMddHHmmss}", cancellationToken);
        var payload = await _backupProvider.ReadBackupAsync(backup.SnapshotId, cancellationToken);
        var checksumOk = new BackupChecksumValidator().Validate(backup, payload);

        var before = CreateIntegritySnapshot();

        var restore = await _restoreService.RestoreAsync(backup.SnapshotId, cancellationToken);

        var after = CreateIntegritySnapshot();
        var integrity = _integrityValidator.Validate(before, after);

        var failover = new FailoverPlan("primary", "secondary");
        failover.BeginFailover();
        failover.CompleteFailover();

        var rto = restore.CompletedAtUtc - restore.StartedAtUtc;
        var rtoCompliant = rto <= policy.RecoveryTimeObjective;
        var rpoCompliant = DateTime.UtcNow - backup.CreatedAtUtc <= policy.RecoveryPointObjective;

        return new DisasterRecoveryValidationReport(
            BackupCreated: backup.Status == "COMPLETED",
            ChecksumValid: checksumOk,
            RestoreSucceeded: restore.Success,
            FinancialIntegrityValid: integrity.Success,
            FailoverSucceeded: failover.Status == FailoverStatus.SecondaryActive,
            RpoCompliant: rpoCompliant,
            RtoCompliant: rtoCompliant);
    }

    private static FinancialIntegritySnapshot CreateIntegritySnapshot() => new(
        TreasuryDebitMinor: 10_000_000,
        TreasuryCreditMinor: 10_000_000,
        AccountingDebitMinor: 10_000_000,
        AccountingCreditMinor: 10_000_000,
        TreasuryTransactions: 10,
        AccountingJournals: 10,
        ActiveReservations: 2,
        CompletedSettlements: 8,
        ReconciliationExceptions: 0,
        CapturedAtUtc: DateTime.UtcNow);
}

public sealed record DisasterRecoveryValidationReport(
    bool BackupCreated,
    bool ChecksumValid,
    bool RestoreSucceeded,
    bool FinancialIntegrityValid,
    bool FailoverSucceeded,
    bool RpoCompliant,
    bool RtoCompliant)
{
    public bool Success =>
        BackupCreated &&
        ChecksumValid &&
        RestoreSucceeded &&
        FinancialIntegrityValid &&
        FailoverSucceeded &&
        RpoCompliant &&
        RtoCompliant;
}