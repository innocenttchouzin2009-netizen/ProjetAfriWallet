using UniversalWallet.Api.Notifications.Domain;

namespace UniversalWallet.Api.Notifications.Application;

public sealed class NotificationPreferencesHandler
{
    private readonly INotificationPreferencesRepository _repository;

    public NotificationPreferencesHandler(INotificationPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotificationPreferences> GetAsync(Guid userAwid, CancellationToken cancellationToken = default)
    {
        return await _repository.GetAsync(userAwid, cancellationToken) ?? new NotificationPreferences { UserAwid = userAwid };
    }

    public async Task<NotificationPreferences> UpdateAsync(UpdatePreferencesRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetAsync(request.UserAwid, cancellationToken) ?? new NotificationPreferences { UserAwid = request.UserAwid };
        if (request.PushEnabled is not null) existing.PushEnabled = request.PushEnabled.Value;
        if (request.EmailEnabled is not null) existing.EmailEnabled = request.EmailEnabled.Value;
        if (request.InAppEnabled is not null) existing.InAppEnabled = request.InAppEnabled.Value;
        if (request.MarketingEnabled is not null) existing.MarketingEnabled = request.MarketingEnabled.Value;
        if (request.SecurityAlertsEnabled is not null) existing.SecurityAlertsEnabled = request.SecurityAlertsEnabled.Value;
        if (request.PaymentAlertsEnabled is not null) existing.PaymentAlertsEnabled = request.PaymentAlertsEnabled.Value;
        if (!string.IsNullOrWhiteSpace(request.Language)) existing.Language = request.Language;
        await _repository.SaveAsync(existing, cancellationToken);
        return existing;
    }
}
