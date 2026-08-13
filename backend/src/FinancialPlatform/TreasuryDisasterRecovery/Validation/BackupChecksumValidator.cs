using System.Security.Cryptography;
using TreasuryDisasterRecovery.Backups;

namespace TreasuryDisasterRecovery.Validation;

public sealed class BackupChecksumValidator
{
    public bool Validate(TreasuryBackupSnapshot snapshot, byte[] payload)
    {
        var actual = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        return string.Equals(snapshot.Sha256, actual, StringComparison.OrdinalIgnoreCase);
    }
}