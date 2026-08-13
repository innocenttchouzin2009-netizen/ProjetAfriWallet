using Execution =
    AfriWallet.BankingPlatform.BankTransferExecution.Domain.Executions.BankTransferExecution;

namespace AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;

public interface IBankTransferExecutionRepository
{
    Task AddAsync(
        Execution execution,
        CancellationToken cancellationToken);

    Task<Execution?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken);

    Task<Execution?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);
}
