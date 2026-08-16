using AfriWallet.Compliance.Screening.Domain.Matching;

namespace AfriWallet.Compliance.Screening.Application.Abstractions;

public interface IScreeningResultRepository
{
    Task AddAsync(
        ScreeningMatch match,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ScreeningMatch>> GetBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default);
}