namespace AfriWallet.Notifications.Domain;

public sealed record NotificationChannelPolicy
{
    private NotificationChannelPolicy(
        NotificationChannel channel,
        bool defaultEnabled,
        bool userConfigurable)
    {
        Channel = channel;
        DefaultEnabled = defaultEnabled;
        UserConfigurable = userConfigurable;
    }

    public NotificationChannel Channel { get; }
    public bool DefaultEnabled { get; }
    public bool UserConfigurable { get; }

    public static NotificationChannelPolicy Create(
        NotificationChannel channel,
        bool defaultEnabled,
        bool userConfigurable)
    {
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        return new NotificationChannelPolicy(channel, defaultEnabled, userConfigurable);
    }
}
