using AfriWallet.Compliance.Screening.Domain.Entries;
using AfriWallet.Compliance.Screening.Domain.Matching;
using AfriWallet.Compliance.Screening.Domain.Subjects;

namespace AfriWallet.Compliance.Screening.Application.Matching;

public sealed class ScreeningMatcher
{
    private readonly ScreeningThresholds _thresholds;

    public ScreeningMatcher(ScreeningThresholds thresholds)
    {
        if (thresholds.ReviewThreshold is < 0 or > 1 ||
            thresholds.BlockThreshold is < 0 or > 1 ||
            thresholds.ReviewThreshold > thresholds.BlockThreshold)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholds));
        }

        _thresholds = thresholds;
    }

    public ScreeningMatch Evaluate(
        ScreeningSubject subject,
        ScreeningEntry entry,
        DateTimeOffset createdAtUtc)
    {
        var normalizedSubjectName = NameNormalizer.Normalize(subject.FullName);
        var candidateNames = entry.Aliases.Prepend(entry.PrimaryName);
        var nameScore = candidateNames
            .Select(name => TokenSimilarity(normalizedSubjectName, NameNormalizer.Normalize(name)))
            .DefaultIfEmpty(0)
            .Max();
        var dateOfBirthScore = ComputeDobScore(subject.DateOfBirth, entry.DateOfBirth);
        var countryScore = ComputeCountryScore(subject.CountryCode, entry.CountryCode);
        var score = Math.Round(
            (nameScore * 0.70) + (dateOfBirthScore * 0.20) + (countryScore * 0.10),
            4,
            MidpointRounding.AwayFromZero);
        var decision = score >= _thresholds.BlockThreshold
            ? ScreeningDecision.Block
            : score >= _thresholds.ReviewThreshold
                ? ScreeningDecision.Review
                : ScreeningDecision.Clear;
        var reasons = new List<string>();

        if (nameScore > 0)
            reasons.Add($"NAME:{nameScore:F4}");
        if (dateOfBirthScore > 0)
            reasons.Add("DATE_OF_BIRTH");
        if (countryScore > 0)
            reasons.Add("COUNTRY");

        return new ScreeningMatch(
            Guid.NewGuid(),
            subject.SubjectId,
            entry.EntryId,
            entry.Type,
            entry.Source.Code,
            score,
            decision,
            reasons,
            createdAtUtc);
    }

    private static double TokenSimilarity(string left, string right)
    {
        if (left == right)
            return 1;

        var leftTokens = left
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
        var rightTokens = right
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        if (leftTokens.Count == 0 || rightTokens.Count == 0)
            return 0;

        var intersection = leftTokens.Intersect(rightTokens).Count();
        var union = leftTokens.Union(rightTokens).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static double ComputeDobScore(DateOnly? left, DateOnly? right) =>
        left.HasValue && right.HasValue && left.Value == right.Value ? 1 : 0;

    private static double ComputeCountryScore(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
}