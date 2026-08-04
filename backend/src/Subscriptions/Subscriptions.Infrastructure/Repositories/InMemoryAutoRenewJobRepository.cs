using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;

namespace Subscriptions.Infrastructure.Repositories;

public sealed class InMemoryAutoRenewJobRepository : IAutoRenewJobRepository
{
    private readonly Dictionary<string, AutoRenewJob> _jobsById = new(StringComparer.OrdinalIgnoreCase);

    public AutoRenewJob Add(AutoRenewJob job)
    {
        if (string.IsNullOrWhiteSpace(job.JobId))
        {
            job.JobId = Guid.NewGuid().ToString("N");
        }

        _jobsById[job.JobId] = job;
        return job;
    }

    public AutoRenewJob Update(AutoRenewJob job)
    {
        _jobsById[job.JobId] = job;
        return job;
    }

    public AutoRenewJob? GetById(string jobId)
    {
        return _jobsById.TryGetValue(jobId, out var job) ? job : null;
    }

    public IReadOnlyList<AutoRenewJob> ListDue(DateTimeOffset asOf)
    {
        return _jobsById.Values
            .Where(job => job.ScheduledFor <= asOf && job.Status != AutoRenewJobStatus.Succeeded && job.Status != AutoRenewJobStatus.Cancelled)
            .OrderBy(job => job.ScheduledFor)
            .ToList();
    }
}
