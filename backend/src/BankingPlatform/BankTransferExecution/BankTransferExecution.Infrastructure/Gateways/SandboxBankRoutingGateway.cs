using AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;

namespace AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Gateways;

public sealed class SandboxBankRoutingGateway
    : IBankRoutingGateway
{
    public Task<RoutingExecutionEligibility>
        GetDecisionAsync(
            Guid routingDecisionId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            new RoutingExecutionEligibility(
                Exists: routingDecisionId != Guid.Empty,
                ProviderCode: "BANK-SANDBOX",
                RailCode: "SEPA"));
    }
}
