using System.Collections.Concurrent;
using AfriWallet.Compliance.Screening.Application.Abstractions;
using AfriWallet.Compliance.Screening.Domain.Matching;

namespace AfriWallet.Compliance.Screening.Infrastructure;

public sealed class InMemoryScreeningResultRepository : IScreeningResultRepository
{
    private readonly ConcurrentQueue<ScreeningMatch> _matches = new();

    public Task AddAsync(
        ScreeningMatch match,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _matches.Enqueue(match);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ScreeningMatch>> GetBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ScreeningMatch> result = _matches
            .Where(match => match.SubjectId == subjectId)
            .OrderByDescending(match => match.Score)
            .ToArray();
        return Task.FromResult(result);
    }
}