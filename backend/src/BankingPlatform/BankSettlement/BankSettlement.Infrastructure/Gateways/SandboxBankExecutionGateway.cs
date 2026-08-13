using AfriWallet.BankingPlatform.BankSettlement.Application;

namespace AfriWallet.BankingPlatform.BankSettlement.Infrastructure.Gateways;

public sealed class SandboxBankExecutionGateway : IBankExecutionGateway
{
    private readonly Dictionary<Guid, BankExecutionStatusSnapshot> _executions = new();

    public SandboxBankExecutionGateway()
    {
        _executions[Guid.Parse("11111111-1111-1111-1111-111111111111")] = new BankExecutionStatusSnapshot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "BANK-ALPHA",
            "SEPA",
            2500,
            50,
            "USD",
            "Completed",
            "bank-alpha-001");

        _executions[Guid.Parse("22222222-2222-2222-2222-222222222222")] = new BankExecutionStatusSnapshot(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "BANK-ALPHA",
            "SEPA",
            1500,
            25,
            "USD",
            "Completed",
            "bank-alpha-002");
    }

    public Task<BankExecutionStatusSnapshot?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _executions.TryGetValue(executionId, out var execution);
        return Task.FromResult(execution);
    }
}
