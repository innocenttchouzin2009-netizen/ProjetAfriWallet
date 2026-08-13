namespace AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;

public interface IBankRoutingGateway
{
    Task<RoutingExecutionEligibility>
        GetDecisionAsync(
            Guid routingDecisionId,
            CancellationToken cancellationToken);
}

public sealed record RoutingExecutionEligibility(
    bool Exists,
    string ProviderCode,
    string RailCode);
