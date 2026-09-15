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

public interface IInAppNotificationRepository
{
    Task<IReadOnlyList<InAppNotification>> ListAsync(Guid userId, bool unreadOnly, int limit, CancellationToken cancellationToken = default);
    Task<InAppNotification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(InAppNotification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(InAppNotification notification, CancellationToken cancellationToken = default);
}

public sealed class InAppNotificationInboxService(IInAppNotificationRepository repository)
{
    public async Task<IReadOnlyList<InAppNotificationSnapshot>> ListAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ValidateUser(userId);
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");
        var items = await repository.ListAsync(userId, unreadOnly, limit, cancellationToken);
        return items.Select(ToSnapshot).ToArray();
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
