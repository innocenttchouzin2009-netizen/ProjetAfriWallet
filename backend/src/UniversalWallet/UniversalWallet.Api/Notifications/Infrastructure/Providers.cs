using UniversalWallet.Api.Notifications.Application;
using UniversalWallet.Api.Notifications.Domain;

namespace UniversalWallet.Api.Notifications.Infrastructure;

public sealed class InAppProvider : INotificationChannelProvider
{
    public string ChannelName => "IN_APP";

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

public sealed class PushProvider : INotificationChannelProvider
{
    public string ChannelName => "PUSH";

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

public sealed class EmailProvider : INotificationChannelProvider
{
    public string ChannelName => "EMAIL";

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
