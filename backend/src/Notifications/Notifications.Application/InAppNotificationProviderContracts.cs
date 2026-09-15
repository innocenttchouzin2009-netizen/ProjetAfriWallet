namespace AfriWallet.Notifications.Application;

public sealed record InAppNotificationMessage(
    Guid NotificationId,
    Guid RecipientUserId,
    string Type,
    string Title,
    string Body,
    DateTimeOffset CreatedAtUtc,
    string? DataJson);

public enum InAppNotificationProviderFailureKind
{
    Transient = 1,
    Permanent = 2
}

public sealed class InAppNotificationProviderException : Exception
{
    public InAppNotificationProviderException(
        InAppNotificationProviderFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public InAppNotificationProviderFailureKind FailureKind { get; }
}

public interface IInAppNotificationProvider
{
    Task PublishAsync(
        InAppNotificationMessage message,
        CancellationToken cancellationToken = default);
}
