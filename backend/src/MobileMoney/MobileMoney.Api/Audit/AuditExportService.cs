namespace MobileMoney.Production.Audit;

public sealed class AuditExportService
{
    public string ExportJson(IReadOnlyList<AuditRecord> records)
    {
        return System.Text.Json.JsonSerializer.Serialize(records, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public string ExportCsv(IReadOnlyList<AuditRecord> records)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("AuditId,TimestampUtc,TransactionId,CorrelationId,Action,Result,PhoneNumber");
        foreach (var record in records)
        {
            sb.AppendLine($"{record.AuditId},{record.TimestampUtc:O},{record.TransactionId},{record.CorrelationId},{record.Action},{record.Result},{MaskPhone(record.PhoneNumber)}");
        }

        return sb.ToString();
    }

    private static string MaskPhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        return phoneNumber.Length <= 4 ? new string('*', phoneNumber.Length) : $"{new string('*', phoneNumber.Length - 4)}{phoneNumber[^4..]}";
    }
}
