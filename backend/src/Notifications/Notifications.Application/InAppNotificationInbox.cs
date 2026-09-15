using System.Text;
using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public sealed record InAppNotificationSnapshot(
    Guid Id,
    Guid PaymentRequestId,
    string Kind,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc,
    Guid? TransferId,
    string Title,
    string Message)
{
    public bool IsRead => ReadAtUtc is not null;
}

public sealed record NotificationInboxCursor
{
    public NotificationInboxCursor(DateTimeOffset createdAtUtc, Guid notificationId)
    {
        if (notificationId == Guid.Empty)
            throw new ArgumentException("Notification id cannot be empty.", nameof(notificationId));
        if (createdAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Cursor timestamp must be UTC.", nameof(createdAtUtc));
        CreatedAtUtc = createdAtUtc;
        NotificationId = notificationId;
    }

    public DateTimeOffset CreatedAtUtc { get; }
    public Guid NotificationId { get; }
}

public sealed record NotificationInboxPage(
    IReadOnlyList<InAppNotificationSnapshot> Items,
    string? NextCursor);

public sealed record NotificationRetentionOptions
{
    public NotificationRetentionOptions(TimeSpan activeInboxRetention)
    {
        if (activeInboxRetention <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(activeInboxRetention));
        ActiveInboxRetention = activeInboxRetention;
    }

    public TimeSpan ActiveInboxRetention { get; }
    public static NotificationRetentionOptions Default { get; } = new(TimeSpan.FromDays(90));
}

public interface IInAppNotificationRepository
{
    Task<bool> AddAsync(InAppNotification notification, CancellationToken cancellationToken = default);
    Task<InAppNotification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InAppNotification>> ListPageAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        NotificationInboxCursor? cursor,
        CancellationToken cancellationToken = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(InAppNotification notification, CancellationToken cancellationToken = default);
    Task<int> ArchiveBeforeAsync(DateTimeOffset cutoffUtc, DateTimeOffset archivedAtUtc, CancellationToken cancellationToken = default);
}

public sealed class InAppNotificationInboxService(IInAppNotificationRepository repository)
{
    public async Task<NotificationInboxPage> ListAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        ValidateUser(userId);
        if (limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");

        var decodedCursor = DecodeCursor(cursor);
        var rows = await repository.ListPageAsync(userId, unreadOnly, checked(limit + 1), decodedCursor, cancellationToken);
        var hasMore = rows.Count > limit;
        var pageRows = rows.Take(limit).ToArray();
        var nextCursor = hasMore && pageRows.Length > 0
            ? EncodeCursor(new NotificationInboxCursor(pageRows[^1].CreatedAtUtc, pageRows[^1].Id))
            : null;

        return new NotificationInboxPage(pageRows.Select(ToSnapshot).ToArray(), nextCursor);
    }

    public async Task<InAppNotificationSnapshot?> GetAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        ValidateUser(userId);
        if (notificationId == Guid.Empty) throw new ArgumentException("Notification id cannot be empty.", nameof(notificationId));
        var item = await repository.GetAsync(userId, notificationId, cancellationToken);
        return item is null ? null : ToSnapshot(item);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ValidateUser(userId);
        return repository.CountUnreadAsync(userId, cancellationToken);
    }

    public async Task<InAppNotificationSnapshot?> MarkReadAsync(
        Guid userId,
        Guid notificationId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateUser(userId);
        if (notificationId == Guid.Empty) throw new ArgumentException("Notification id cannot be empty.", nameof(notificationId));
        var item = await repository.GetAsync(userId, notificationId, cancellationToken);
        if (item is null) return null;
        if (item.MarkRead(readAtUtc)) await repository.UpdateAsync(item, cancellationToken);
        return ToSnapshot(item);
    }

    internal static string EncodeCursor(NotificationInboxCursor cursor)
    {
        var payload = $"{cursor.CreatedAtUtc.UtcTicks:D19}|{cursor.NotificationId:N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    internal static NotificationInboxCursor? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var base64 = cursor.Trim().Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = payload.Split('|');
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], out var utcTicks) ||
                !Guid.TryParseExact(parts[1], "N", out var notificationId))
                throw new FormatException();
            return new NotificationInboxCursor(new DateTimeOffset(utcTicks, TimeSpan.Zero), notificationId);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or ArgumentOutOfRangeException)
        {
            throw new ArgumentException("Notification cursor is invalid.", nameof(cursor));
        }
    }

    private static void ValidateUser(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
    }

    private static InAppNotificationSnapshot ToSnapshot(InAppNotification item)
    {
        var (title, message) = item.EventKind switch
        {
            PaymentRequestEventKind.Created => ("Payment request received", "You received a new payment request."),
            PaymentRequestEventKind.Accepted => ("Payment request accepted", "A payment request was accepted."),
            PaymentRequestEventKind.Declined => ("Payment request declined", "A payment request was declined."),
            PaymentRequestEventKind.Cancelled => ("Payment request cancelled", "A payment request was cancelled."),
            PaymentRequestEventKind.Expired => ("Payment request expired", "A payment request expired."),
            PaymentRequestEventKind.Paid => ("Payment request paid", "A payment request was paid."),
            _ => throw new ArgumentOutOfRangeException()
        };

        return new InAppNotificationSnapshot(
            item.Id,
            item.PaymentRequestId,
            item.EventKind.ToString(),
            item.CreatedAtUtc,
            item.ReadAtUtc,
            item.TransferId,
            title,
            message);
    }
}

public sealed class NotificationRetentionService(
    IInAppNotificationRepository repository,
    NotificationRetentionOptions options)
{
    public async Task<int> ArchiveExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        if (nowUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Current timestamp must be UTC.", nameof(nowUtc));
        var cutoff = nowUtc.Subtract(options.ActiveInboxRetention);
        return await repository.ArchiveBeforeAsync(cutoff, nowUtc, cancellationToken);
    }
}
