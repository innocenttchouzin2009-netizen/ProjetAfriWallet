namespace MobileMoney.Production.Audit;

public static class AuditChainValidator
{
    public static bool IsChainValid(IReadOnlyList<AuditRecord> records)
    {
        for (var i = 1; i < records.Count; i++)
        {
            var previous = records[i - 1];
            var current = records[i];
            if (previous.CurrentAuditHash is null || current.PreviousAuditHash is null)
            {
                return false;
            }

            if (!string.Equals(previous.CurrentAuditHash, current.PreviousAuditHash, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
