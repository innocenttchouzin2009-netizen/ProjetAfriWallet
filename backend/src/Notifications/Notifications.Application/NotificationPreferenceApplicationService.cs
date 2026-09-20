using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public sealed class NotificationPreferenceApplicationService(
    INotificationPreferenceRepository repository,
    INotificationChannelPolicyProvider policyProvider)
{
    public async Task<IReadOnlyList<NotificationPreferenceSnapshot>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        cancellationToken.ThrowIfCancellationRequested();
        var existing = await repository.ListByUserAsync(userId, cancellationToken);
        var byChannel = existing.ToDictionary(x => x.Channel);
        var result = new List<NotificationPreference>();

        foreach (var policy in policyProvider.List().OrderBy(x => x.Channel))
        {
            if (byChannel.TryGetValue(policy.Channel, out var preference))
            {
                result.Add(preference);
                continue;
            }

            var created = NotificationPreference.New(userId, policy, DateTimeOffset.UtcNow);
            await repository.AddAsync(created, cancellationToken);
            result.Add(created);
        }

        return result.Select(NotificationPreferenceMappings.ToSnapshot).ToArray();
    }

    public async Task<UpdateNotificationPreferenceResult> UpdateAsync(
        UpdateNotificationPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(command));
        if (!Enum.IsDefined(command.Channel))
            throw new ArgumentOutOfRangeException(nameof(command), "Notification channel is invalid.");
        if (command.UpdatedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", nameof(command));

        cancellationToken.ThrowIfCancellationRequested();
        var policy = policyProvider.Get(command.Channel);
        var preference = await repository.GetAsync(command.UserId, command.Channel, cancellationToken);

        if (preference is null)
        {
            preference = NotificationPreference.New(command.UserId, policy, command.UpdatedAtUtc);
            var changedOnCreate = preference.SetEnabled(command.IsEnabled, policy, command.UpdatedAtUtc);
            await repository.AddAsync(preference, cancellationToken);
            return new UpdateNotificationPreferenceResult(
                UpdateNotificationPreferenceStatus.Created,
                NotificationPreferenceMappings.ToSnapshot(preference));
        }

        var changed = preference.SetEnabled(command.IsEnabled, policy, command.UpdatedAtUtc);
        if (!changed)
        {
            return new UpdateNotificationPreferenceResult(
                UpdateNotificationPreferenceStatus.Unchanged,
                NotificationPreferenceMappings.ToSnapshot(preference));
        }

        await repository.UpdateAsync(preference, cancellationToken);
        return new UpdateNotificationPreferenceResult(
            UpdateNotificationPreferenceStatus.Updated,
            NotificationPreferenceMappings.ToSnapshot(preference));
    }
}
