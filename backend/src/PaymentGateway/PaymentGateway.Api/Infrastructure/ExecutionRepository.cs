using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Infrastructure;

public interface IExecutionRepository
{
    void Save(TransferExecution execution);
    TransferExecution? Get(Guid id);
    IReadOnlyCollection<TransferExecution> List();
}

public sealed class InMemoryExecutionRepository : IExecutionRepository
{
    private readonly Dictionary<Guid, TransferExecution> _executions = new();

    public void Save(TransferExecution execution) => _executions[execution.Id] = execution;

    public TransferExecution? Get(Guid id) => _executions.TryGetValue(id, out var execution) ? execution : null;

    public IReadOnlyCollection<TransferExecution> List() => _executions.Values.ToList();
}
