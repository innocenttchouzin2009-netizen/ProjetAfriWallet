using AfriWallet.Compliance.Screening.Application.Abstractions;
using AfriWallet.Compliance.Screening.Domain.Entries;

namespace AfriWallet.Compliance.Screening.Infrastructure.Providers;

public sealed class SandboxSanctionsProvider : IScreeningListProvider
{
    public ScreeningSource Source { get; } =
        new(
            "SANCTIONS-SBX",
            "AfriWallet Sandbox Sanctions",
            Sandbox: true);

    public Task<IReadOnlyCollection<ScreeningEntry>> GetEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ScreeningEntry> result =
        [
            new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ScreeningEntryType.Sanctions,
                "TEST BLOCKED PERSON",
                ["BLOCKED TEST PERSON"],
                new DateOnly(1980, 1, 1),
                "CM",
                Source),
            new(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ScreeningEntryType.Sanctions,
                "SANDBOX WATCH NAME",
                [],
                null,
                "DE",
                Source)
        ];

        return Task.FromResult(result);
    }
}