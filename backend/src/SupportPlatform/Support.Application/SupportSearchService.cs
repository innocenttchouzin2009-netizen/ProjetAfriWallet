using Support.Contracts;
using Support.Domain;

namespace Support.Application;

public sealed class SupportSearchService
{
    public IReadOnlyList<SupportCase> Search(IReadOnlyList<SupportCase> supportCases, SupportCaseQuery query)
    {
        IEnumerable<SupportCase> filtered = supportCases;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ParsingExtensions.ParseStatus(query.Status);
            filtered = filtered.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            var priority = ParsingExtensions.ParsePriority(query.Priority);
            filtered = filtered.Where(x => x.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = ParsingExtensions.ParseCategory(query.Category);
            filtered = filtered.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query.AssignedTeam))
        {
            filtered = filtered.Where(x => string.Equals(x.AssignedTeam, query.AssignedTeam, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.OrderByDescending(x => x.UpdatedAtUtc).ToList();
    }
}
