namespace MobileMoney.Production.Audit;

public interface IAuditService
{
    AuditRecord Record(AuditRecord record);
    AuditRecord Record(string action, AuditResult result, AuditCategory category, AuditContext context);
    IReadOnlyList<AuditRecord> Search(AuditSearchCriteria criteria);
    AuditRecord? GetById(string auditId);
    IReadOnlyList<AuditRecord> Export(AuditExportFilter filter);
    bool VerifyChain(string auditId);
}
