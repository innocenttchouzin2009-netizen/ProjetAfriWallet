using UniversalWallet.Api.Notifications.Application;
using UniversalWallet.Api.Notifications.Domain;

namespace UniversalWallet.Api.Notifications.Api;

public static class NotificationEndpoints
{
    public static WebApplication MapNotificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/notifications", async (Guid userAwid, INotificationRepository repository) =>
        {
            var items = await repository.ListAsync(userAwid);
            return Results.Ok(new { items });
        });

        app.MapGet("/api/v1/notifications/{id:guid}", async (Guid id, INotificationRepository repository) =>
        {
            var notification = await repository.GetAsync(id);
            return notification is null ? Results.NotFound(new { code = "NOTIFICATION_NOT_FOUND", message = "Notification not found." }) : Results.Ok(notification);
        });

        app.MapPost("/api/v1/notifications/{id:guid}/read", async (Guid id, INotificationRepository repository) =>
        {
            var notification = await repository.GetAsync(id);
            if (notification is null)
            {
                return Results.NotFound(new { code = "NOTIFICATION_NOT_FOUND", message = "Notification not found." });
            }

            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await repository.UpdateAsync(notification);
            return Results.Ok(notification);
        });

        app.MapPost("/api/v1/notifications/read-all", async (Guid userAwid, INotificationRepository repository) =>
        {
            var notifications = await repository.ListAsync(userAwid);
            foreach (var notification in notifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTimeOffset.UtcNow;
                await repository.UpdateAsync(notification);
            }
            return Results.Ok(new { updated = notifications.Count });
        });

        app.MapGet("/api/v1/notification-preferences", async (Guid userAwid, INotificationPreferencesRepository repository) =>
        {
            var preferences = await repository.GetAsync(userAwid);
            return preferences is null ? Results.Ok(new NotificationPreferences { UserAwid = userAwid }) : Results.Ok(preferences);
        });

        app.MapPut("/api/v1/notification-preferences", async (UpdatePreferencesRequest request, NotificationPreferencesHandler handler) =>
        {
            var preferences = await handler.UpdateAsync(request);
            return Results.Ok(preferences);
        });

        return app;
    }
}
