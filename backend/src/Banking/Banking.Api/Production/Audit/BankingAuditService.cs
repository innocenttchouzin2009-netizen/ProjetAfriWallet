namespace AfriWallet.Banking.Api.Production.Audit;

public sealed class BankingAuditService
{
    public void Record(string action, string subjectId, string correlationId)
    {
        Console.WriteLine($"AUDIT action={action} subjectId={subjectId} correlationId={correlationId}");
    }
}
