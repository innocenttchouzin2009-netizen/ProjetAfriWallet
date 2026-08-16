using AfriWallet.Compliance.Screening.Application.Abstractions;
using AfriWallet.Compliance.Screening.Domain.Entries;

namespace AfriWallet.Compliance.Screening.Infrastructure.Providers;

public sealed class SandboxPepProvider : IScreeningListProvider
{
    public ScreeningSource Source { get; } =
        new(
            "PEP-SBX",
            "AfriWallet Sandbox PEP",
            Sandbox: true);

    public Task<IReadOnlyCollection<ScreeningEntry>> GetEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ScreeningEntry> result =
        [
            new(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ScreeningEntryType.Pep,
                "TEST POLITICAL PERSON",
                ["POLITICAL TEST PERSON"],
                new DateOnly(1975, 5, 20),
                "FR",
                Source)
        ];

        return Task.FromResult(result);
    }
}