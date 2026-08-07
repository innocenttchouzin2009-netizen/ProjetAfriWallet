namespace TreasuryDisasterRecovery.Validation;

public sealed record DisasterRecoveryPolicy(
    TimeSpan RecoveryPointObjective,
    TimeSpan RecoveryTimeObjective,
    int MinimumRetainedBackups,
    bool RequireEncryptedBackups,
    bool RequireChecksumValidation)
{
    public static DisasterRecoveryPolicy ProductionDefault =>
        new(
            RecoveryPointObjective: TimeSpan.FromMinutes(15),
            RecoveryTimeObjective: TimeSpan.FromHours(1),
            MinimumRetainedBackups: 3,
            RequireEncryptedBackups: true,
            RequireChecksumValidation: true);
}