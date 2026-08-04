using UniversalWallet.Api.Notifications.Domain;

namespace UniversalWallet.Api.Notifications.Application;

public sealed class CreateNotificationHandler
{
    private readonly INotificationRepository _repository;
    private readonly INotificationPreferencesRepository _preferencesRepository;
    private readonly IEnumerable<INotificationChannelProvider> _providers;

    public CreateNotificationHandler(
        INotificationRepository repository,
        INotificationPreferencesRepository preferencesRepository,
        IEnumerable<INotificationChannelProvider> providers)
    {
        _repository = repository;
        _preferencesRepository = preferencesRepository;
        _providers = providers;
    }

    public async Task<CreateNotificationResponse> HandleAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var key = request.NotificationKey ?? BuildKey(request.EventType, request.UserAwid, "IN_APP");
        var existing = await _repository.GetByKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            return new CreateNotificationResponse(existing);
        }

        var notification = new Notification
        {
            UserAwid = request.UserAwid,
            EventType = request.EventType,
            Category = request.Category,
            Priority = request.Priority,
            Title = request.Title,
            Body = request.Body,
            Payload = request.Payload,
            CorrelationId = request.CorrelationId,
            NotificationKey = key,
            Status = NotificationStatus.Queued,
            Version = 1
        };

        await _repository.AddAsync(notification, cancellationToken);

        var preferences = await _preferencesRepository.GetAsync(request.UserAwid, cancellationToken) ?? new NotificationPreferences { UserAwid = request.UserAwid };
        if (preferences.InAppEnabled)
        {
            foreach (var provider in _providers.Where(p => p.ChannelName is "IN_APP" or "PUSH" or "EMAIL"))
            {
                var shouldSend = provider.ChannelName switch
                {
                    "PUSH" => preferences.PushEnabled,
                    "EMAIL" => preferences.EmailEnabled,
                    _ => preferences.InAppEnabled
                };

                if (!shouldSend)
                {
                    continue;
                }

                notification.Status = NotificationStatus.Sending;
                await _repository.UpdateAsync(notification, cancellationToken);
                var delivered = await provider.SendAsync(notification, cancellationToken);
                notification.Status = delivered ? NotificationStatus.Sent : NotificationStatus.Failed;
                await _repository.UpdateAsync(notification, cancellationToken);
            }
        }

        return new CreateNotificationResponse(notification);
    }

    private static string BuildKey(string eventType, Guid userAwid, string channel) => $"{eventType}:{userAwid:N}:{channel}";
}
