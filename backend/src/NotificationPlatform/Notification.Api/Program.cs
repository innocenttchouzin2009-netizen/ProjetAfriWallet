using Notification.Application;
using Notification.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TemplateEngine>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddSingleton<RetryService>();
builder.Services.AddSingleton<PreferenceService>();
builder.Services.AddSingleton<DeliveryDispatcher>();
builder.Services.AddSingleton<NotificationService>();

var app = builder.Build();

app.MapPost("/api/v1/notifications", (CreateNotificationRequest request, NotificationService service) => Results.Ok(service.CreateNotification(request)));
app.MapGet("/api/v1/notifications/{notificationId:guid}", (Guid notificationId, NotificationService service) => Results.Ok(service.GetNotification(notificationId)));
app.MapGet("/api/v1/notifications/preferences", (string awid, NotificationService service) => Results.Ok(service.GetPreferences(awid)));
app.MapPut("/api/v1/notifications/preferences", (NotificationPreferenceRequest request, NotificationService service) => Results.Ok(service.UpdatePreferences(request)));
app.MapGet("/api/v1/notifications/templates", (NotificationService service) => Results.Ok(service.ListTemplates()));
app.MapPost("/api/v1/notifications/templates", (CreateTemplateRequest request, NotificationService service) => Results.Ok(service.CreateTemplate(request)));
app.MapPost("/api/v1/notifications/templates/{templateId:guid}/publish", (Guid templateId, PublishTemplateRequest request, NotificationService service) => Results.Ok(service.PublishTemplate(templateId, request)));

app.Run();
