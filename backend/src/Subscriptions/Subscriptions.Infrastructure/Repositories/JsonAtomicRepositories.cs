using System.Text;
using System.Text.Json;
using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;

namespace Subscriptions.Infrastructure.Repositories;

internal static class JsonFileHelpers
{
    public static string ReadUtf8Text(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    public static void WriteUtf8Atomic(string path, string contents)
    {
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, contents, new UTF8Encoding(false));
        File.Move(tempPath, path, true);
    }
}

public sealed class JsonAtomicSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly object _gate = new();
    private readonly Dictionary<string, UserSubscription> _subscriptionsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string UserId, string ProviderId), string> _index = new();

    public JsonAtomicSubscriptionRepository(string storageRoot)
    {
        _storagePath = Path.Combine(storageRoot, "subscriptions.json");
        Directory.CreateDirectory(storageRoot);
        Load();
    }

    public UserSubscription Add(UserSubscription subscription)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(subscription.SubscriptionId))
            {
                subscription.SubscriptionId = Guid.NewGuid().ToString("N");
            }

            _subscriptionsById[subscription.SubscriptionId] = subscription;
            _index[(subscription.UserId, subscription.ProviderId)] = subscription.SubscriptionId;
            Persist();
            return subscription;
        }
    }

    public UserSubscription Update(UserSubscription subscription)
    {
        lock (_gate)
        {
            _subscriptionsById[subscription.SubscriptionId] = subscription;
            _index[(subscription.UserId, subscription.ProviderId)] = subscription.SubscriptionId;
            Persist();
            return subscription;
        }
    }

    public UserSubscription? GetById(string subscriptionId)
    {
        lock (_gate)
        {
            return _subscriptionsById.TryGetValue(subscriptionId, out var subscription) ? subscription : null;
        }
    }

    public UserSubscription? FindByUserAndProvider(string userId, string providerId)
    {
        lock (_gate)
        {
            if (_index.TryGetValue((userId, providerId), out var subscriptionId))
            {
                return _subscriptionsById[subscriptionId];
            }

            return null;
        }
    }

    private void Load()
    {
        if (!File.Exists(_storagePath))
        {
            return;
        }

        lock (_gate)
        {
            if (!File.Exists(_storagePath))
            {
                return;
            }

            var text = JsonFileHelpers.ReadUtf8Text(_storagePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<List<UserSubscription>>(text, _options) ?? new List<UserSubscription>();
                _subscriptionsById.Clear();
                _index.Clear();
                foreach (var subscription in payload)
                {
                    _subscriptionsById[subscription.SubscriptionId] = subscription;
                    _index[(subscription.UserId, subscription.ProviderId)] = subscription.SubscriptionId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Subscription load failed: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(text);
                _subscriptionsById.Clear();
                _index.Clear();
            }
        }
    }

    private void Persist()
    {
        var payload = _subscriptionsById.Values.ToList();
        var serialized = JsonSerializer.Serialize(payload, _options);
        JsonFileHelpers.WriteUtf8Atomic(_storagePath, serialized);
    }
}

public sealed class JsonAtomicSubscriptionInvoiceRepository : ISubscriptionInvoiceRepository
{
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly object _gate = new();
    private readonly Dictionary<string, SubscriptionInvoice> _invoicesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string SubscriptionId, DateTimeOffset Start, DateTimeOffset End), string> _index = new();

    public JsonAtomicSubscriptionInvoiceRepository(string storageRoot)
    {
        _storagePath = Path.Combine(storageRoot, "invoices.json");
        Directory.CreateDirectory(storageRoot);
        Load();
    }

    public SubscriptionInvoice Add(SubscriptionInvoice invoice)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(invoice.InvoiceId))
            {
                invoice.InvoiceId = Guid.NewGuid().ToString("N");
            }

            _invoicesById[invoice.InvoiceId] = invoice;
            var normalizedStart = Normalize(invoice.BillingPeriodStart);
            var normalizedEnd = Normalize(invoice.BillingPeriodEnd);
            _index[(invoice.SubscriptionId, normalizedStart, normalizedEnd)] = invoice.InvoiceId;
            Persist();
            return invoice;
        }
    }

    public SubscriptionInvoice Update(SubscriptionInvoice invoice)
    {
        lock (_gate)
        {
            _invoicesById[invoice.InvoiceId] = invoice;
            var normalizedStart = Normalize(invoice.BillingPeriodStart);
            var normalizedEnd = Normalize(invoice.BillingPeriodEnd);
            _index[(invoice.SubscriptionId, normalizedStart, normalizedEnd)] = invoice.InvoiceId;
            Persist();
            return invoice;
        }
    }

    public SubscriptionInvoice? GetById(string invoiceId)
    {
        lock (_gate)
        {
            return _invoicesById.TryGetValue(invoiceId, out var invoice) ? invoice : null;
        }
    }

    public SubscriptionInvoice? GetBySubscriptionAndPeriod(string subscriptionId, DateTimeOffset start, DateTimeOffset end)
    {
        lock (_gate)
        {
            var normalizedStart = Normalize(start);
            var normalizedEnd = Normalize(end);
            if (_index.TryGetValue((subscriptionId, normalizedStart, normalizedEnd), out var invoiceId))
            {
                return _invoicesById[invoiceId];
            }

            return _invoicesById.Values.FirstOrDefault(invoice =>
                invoice.SubscriptionId.Equals(subscriptionId, StringComparison.OrdinalIgnoreCase) &&
                Normalize(invoice.BillingPeriodStart) == normalizedStart &&
                Normalize(invoice.BillingPeriodEnd) == normalizedEnd);
        }
    }

    private void Load()
    {
        if (!File.Exists(_storagePath))
        {
            return;
        }

        lock (_gate)
        {
            if (!File.Exists(_storagePath))
            {
                return;
            }

            var text = JsonFileHelpers.ReadUtf8Text(_storagePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<List<SubscriptionInvoice>>(text, _options) ?? new List<SubscriptionInvoice>();
                _invoicesById.Clear();
                _index.Clear();
                foreach (var invoice in payload)
                {
                    _invoicesById[invoice.InvoiceId] = invoice;
                    var normalizedStart = Normalize(invoice.BillingPeriodStart);
                    var normalizedEnd = Normalize(invoice.BillingPeriodEnd);
                    _index[(invoice.SubscriptionId, normalizedStart, normalizedEnd)] = invoice.InvoiceId;
                }
            }
            catch (JsonException)
            {
                _invoicesById.Clear();
                _index.Clear();
            }
        }
    }

    private void Persist()
    {
        var payload = _invoicesById.Values.ToList();
        var serialized = JsonSerializer.Serialize(payload, _options);
        JsonFileHelpers.WriteUtf8Atomic(_storagePath, serialized);
    }

    private static DateTimeOffset Normalize(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Offset);
    }
}

public sealed class JsonAtomicAutoRenewJobRepository : IAutoRenewJobRepository
{
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly object _gate = new();
    private readonly Dictionary<string, AutoRenewJob> _jobsById = new(StringComparer.OrdinalIgnoreCase);

    public JsonAtomicAutoRenewJobRepository(string storageRoot)
    {
        _storagePath = Path.Combine(storageRoot, "auto-renew-jobs.json");
        Directory.CreateDirectory(storageRoot);
        Load();
    }

    public AutoRenewJob Add(AutoRenewJob job)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(job.JobId))
            {
                job.JobId = Guid.NewGuid().ToString("N");
            }

            _jobsById[job.JobId] = job;
            Persist();
            return job;
        }
    }

    public AutoRenewJob Update(AutoRenewJob job)
    {
        lock (_gate)
        {
            _jobsById[job.JobId] = job;
            Persist();
            return job;
        }
    }

    public AutoRenewJob? GetById(string jobId)
    {
        lock (_gate)
        {
            return _jobsById.TryGetValue(jobId, out var job) ? job : null;
        }
    }

    public IReadOnlyList<AutoRenewJob> ListDue(DateTimeOffset asOf)
    {
        lock (_gate)
        {
            return _jobsById.Values
                .Where(job => job.ScheduledFor <= asOf && job.Status != AutoRenewJobStatus.Succeeded && job.Status != AutoRenewJobStatus.Cancelled)
                .OrderBy(job => job.ScheduledFor)
                .ToList();
        }
    }

    private void Load()
    {
        if (!File.Exists(_storagePath))
        {
            return;
        }

        lock (_gate)
        {
            if (!File.Exists(_storagePath))
            {
                return;
            }

            var text = JsonFileHelpers.ReadUtf8Text(_storagePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<List<AutoRenewJob>>(text, _options) ?? new List<AutoRenewJob>();
                _jobsById.Clear();
                foreach (var job in payload)
                {
                    _jobsById[job.JobId] = job;
                }
            }
            catch (JsonException)
            {
                _jobsById.Clear();
            }
        }
    }

    private void Persist()
    {
        var payload = _jobsById.Values.ToList();
        var serialized = JsonSerializer.Serialize(payload, _options);
        JsonFileHelpers.WriteUtf8Atomic(_storagePath, serialized);
    }
}
