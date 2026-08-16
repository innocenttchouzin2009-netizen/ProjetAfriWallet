using AfriWallet.Compliance.Screening.Domain.Entries;

namespace AfriWallet.Compliance.Screening.Application.Abstractions;

public interface IScreeningListProvider
{
    ScreeningSource Source { get; }

    Task<IReadOnlyCollection<ScreeningEntry>> GetEntriesAsync(
        CancellationToken cancellationToken = default);
}