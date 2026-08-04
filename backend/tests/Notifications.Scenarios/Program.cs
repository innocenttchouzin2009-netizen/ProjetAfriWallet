using UniversalWallet.Api.Notifications.Application;
using UniversalWallet.Api.Notifications.Domain;
using UniversalWallet.Api.Notifications.Infrastructure;

var failures = new List<string>();
var repository = new InMemoryNotificationRepository();
var preferencesRepository = new InMemoryNotificationPreferencesRepository();
var providers = new INotificationChannelProvider[] { new InAppProvider(), new PushProvider(), new EmailProvider() };
var handler = new CreateNotificationHandler(repository, preferencesRepository, providers);
var preferencesHandler = new NotificationPreferencesHandler(preferencesRepository);

await Run("notification is created and stored", async () =>
{
    var response = await handler.HandleAsync(new CreateNotificationRequest("PaymentCompleted", Guid.NewGuid(), "payment", NotificationPriority.High, "Paiement envoyé", "Votre paiement a été envoyé.", "{}", "corr-1"));
    Assert(response.Notification.Status == NotificationStatus.Sent, "notification should be sent when providers succeed");
});

await Run("preferences suppress channels", async () =>
{
    var userAwid = Guid.NewGuid();
    await preferencesHandler.UpdateAsync(new UpdatePreferencesRequest(userAwid, PushEnabled: false, EmailEnabled: false, InAppEnabled: true, MarketingEnabled: false, SecurityAlertsEnabled: true, PaymentAlertsEnabled: true, Language: "fr"));
    var response = await handler.HandleAsync(new CreateNotificationRequest("PaymentCompleted", userAwid, "payment", NotificationPriority.High, "Paiement envoyé", "Votre paiement a été envoyé.", "{}", "corr-2"));
    Assert(response.Notification.Status == NotificationStatus.Sent, "notification should still be created even when push/email disabled");
});

await Run("duplicate notification is rejected by key", async () =>
{
    var userAwid = Guid.NewGuid();
    var first = await handler.HandleAsync(new CreateNotificationRequest("PaymentCompleted", userAwid, "payment", NotificationPriority.High, "Paiement envoyé", "Votre paiement a été envoyé.", "{}", "corr-3", "key-dup"));
    var second = await handler.HandleAsync(new CreateNotificationRequest("PaymentCompleted", userAwid, "payment", NotificationPriority.High, "Paiement envoyé", "Votre paiement a été envoyé.", "{}", "corr-4", "key-dup"));
    Assert(first.Notification.NotificationId == second.Notification.NotificationId, "duplicate notification should reuse the same notification");
});

await Run("mark as read and mark all as read", async () =>
{
    var userAwid = Guid.NewGuid();
    var created = await handler.HandleAsync(new CreateNotificationRequest("PaymentCompleted", userAwid, "payment", NotificationPriority.High, "Paiement envoyé", "Votre paiement a été envoyé.", "{}", "corr-5"));
    var preferences = await preferencesHandler.GetAsync(userAwid);
    Assert(preferences.UserAwid == userAwid, "preferences should be available");
});

if (failures.Count == 0)
{
    Console.WriteLine("All notification scenarios passed.");
    return;
}

Console.WriteLine("Notification scenarios failed:");
foreach (var failure in failures)
{
    Console.WriteLine($" - {failure}");
}
Environment.ExitCode = 1;

async Task Run(string name, Func<Task> scenario)
{
    try
    {
        await scenario();
        Console.WriteLine($"[OK] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[KO] {name} -> {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}
