using AfriWallet.Compliance.Screening.Application.Abstractions;
using AfriWallet.Compliance.Screening.Application.Matching;
using AfriWallet.Compliance.Screening.Domain.Matching;

namespace AfriWallet.Compliance.Screening.Application.Screening;

public sealed class ScreeningService
{
    private readonly IScreeningProviderRegistry _providers;
    private readonly IScreeningResultRepository _results;
    private readonly IScreeningAuditStore _audit;
    private readonly IScreeningClock _clock;
    private readonly ScreeningMatcher _matcher;

    public ScreeningService(
        IScreeningProviderRegistry providers,
        IScreeningResultRepository results,
        IScreeningAuditStore audit,
        IScreeningClock clock,
        ScreeningMatcher matcher)
    {
        _providers = providers;
        _results = results;
        _audit = audit;
        _clock = clock;
        _matcher = matcher;
    }

    public async Task<ScreeningResult> ScreenAsync(
        ScreenSubjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var matches = new List<ScreeningMatch>();

        foreach (var provider in _providers.All())
        {
            if (!provider.Source.Sandbox)
            {
                throw new InvalidOperationException(
                    "AFW-DLV-0016.3 permits sandbox screening sources only.");
            }

            var entries = await provider.GetEntriesAsync(cancellationToken);
            foreach (var entry in entries)
            {
                var match = _matcher.Evaluate(command.Subject, entry, _clock.UtcNow);
                if (match.Decision == ScreeningDecision.Clear)
                    continue;

                await _results.AddAsync(match, cancellationToken);
                matches.Add(match);
            }
        }

        var decision = matches.Any(match => match.Decision == ScreeningDecision.Block)
            ? ScreeningDecision.Block
            : matches.Any(match => match.Decision == ScreeningDecision.Review)
                ? ScreeningDecision.Review
                : ScreeningDecision.Clear;

        await _audit.AppendAsync(
            new ScreeningAuditEvent(
                Guid.NewGuid(),
                command.Subject.SubjectId,
                "screening.completed",
                command.Actor,
                _clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["decision"] = decision.ToString(),
                    ["matchCount"] = matches.Count.ToString()
                }),
            cancellationToken);

        return new ScreeningResult(command.Subject.SubjectId, decision, matches);
    }
}