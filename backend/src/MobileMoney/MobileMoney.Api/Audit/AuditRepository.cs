namespace MobileMoney.Production.Audit;

public sealed class AuditRepository
{
    private readonly List<AuditRecord> _records = new();

    public AuditRecord Save(AuditRecord record)
    {
        _records.Add(record);
        return record;
    }

    public AuditRecord? GetById(string auditId) => _records.FirstOrDefault(x => x.AuditId == auditId);

    public IReadOnlyList<AuditRecord> Search(Func<AuditRecord, bool> predicate) => _records.Where(predicate).ToList();

    public IReadOnlyList<AuditRecord> All() => _records;
}
