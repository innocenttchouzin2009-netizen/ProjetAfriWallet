using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;
using AfriWallet.Fraud.TransactionFraud.Domain.Signals;

namespace AfriWallet.Fraud.TransactionFraud.Infrastructure;

public sealed class SandboxFraudSignalReader : IFraudSignalReader
{
    private readonly List<FraudSignalSnapshot> _signals = [];

    public void Add(FraudSignalSnapshot signal)
    {
        _signals.Add(signal);
    }

    public Task<IReadOnlyCollection<FraudSignalSnapshot>> GetBySubjectAsync(
        string subjectType,
        string subjectId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<FraudSignalSnapshot> result = _signals
            .Where(x =>
                string.Equals(x.SubjectType, subjectType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.SubjectId, subjectId, StringComparison.Ordinal) &&
                x.OccurredAtUtc >= fromUtc)
            .OrderBy(x => x.OccurredAtUtc)
            .ToArray();

        return Task.FromResult(result);
    }
}
