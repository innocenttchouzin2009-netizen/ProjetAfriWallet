using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using AfriWallet.PaymentRequests.Application;
using IdentityService.Api.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-notifications-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";

try
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddInAppNotifications(connectionString);

    Assert(builder.Services.Any(x => x.ServiceType == typeof(IPaymentRequestEventTransport) && x.ImplementationType == typeof(NotificationPaymentRequestEventTransport)),
        "Composition must wire the durable payment request event transport to the in-app inbox.");
    Assert(builder.Services.Any(x => x.ServiceType == typeof(NotificationRetentionService)),
        "Composition must wire notification retention service.");

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapNotificationEndpoints();
    await app.StartAsync();

    var userId = Guid.NewGuid();
    var foreignUserId = Guid.NewGuid();
    var createdAt = new DateTimeOffset(2026, 9, 15, 20, 0, 0, TimeSpan.Zero);

    Guid firstId;
    Guid secondId;
    Guid foreignId;
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<NotificationInboxDbContext>();
        await db.Database.EnsureCreatedAsync();
        var repository = scope.ServiceProvider.GetRequiredService<IInAppNotificationRepository>();

        var first = InAppNotification.New(userId, PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Created, createdAt));
        var second = InAppNotification.New(userId, PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Accepted, createdAt.AddMinutes(1)));
        second.MarkRead(createdAt.AddMinutes(2));
        var foreign = InAppNotification.New(foreignUserId, PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Declined, createdAt.AddMinutes(2)));

        await repository.AddAsync(first);
        await repository.AddAsync(second);
        await repository.AddAsync(foreign);
        await repository.UpdateAsync(second);

        firstId = first.Id;
        secondId = second.Id;
        foreignId = foreign.Id;
    }

    var anonymous = app.GetTestClient();
    var client = CreateClient(app, userId);

    var anonymousResponse = await anonymous.GetAsync("/api/v1/notifications");
    Assert(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, "Notification inbox must require authentication.");

    var firstPageResponse = await client.GetAsync("/api/v1/notifications?limit=1");
    Assert(firstPageResponse.StatusCode == HttpStatusCode.OK, "Authenticated inbox page must return 200.");
    var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<NotificationInboxPage>();
    Assert(firstPage is { Items.Count: 1 } && !string.IsNullOrWhiteSpace(firstPage.NextCursor), "First page must expose one item and a next cursor.");

    var secondPageResponse = await client.GetAsync($"/api/v1/notifications?limit=1&cursor={Uri.EscapeDataString(firstPage!.NextCursor!)}");
    var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<NotificationInboxPage>();
    Assert(secondPage is { Items.Count: 1 }, "Second page must contain the remaining owned notification.");
    Assert(secondPage!.Items[0].Id != firstPage.Items[0].Id, "Cursor pagination must not repeat items.");
    Assert(secondPage.NextCursor is null, "Final page must not emit another cursor.");

    var invalidCursor = await client.GetAsync("/api/v1/notifications?cursor=not-a-valid-cursor");
    Assert(invalidCursor.StatusCode == HttpStatusCode.BadRequest, "Malformed cursor must return 400.");

    var unreadResponse = await client.GetAsync("/api/v1/notifications?unreadOnly=true&limit=50");
    var unread = await unreadResponse.Content.ReadFromJsonAsync<NotificationInboxPage>();
    Assert(unread is { Items.Count: 1 } && unread.Items[0].Id == firstId, "Unread filter must return only unread notifications.");

    var countResponse = await client.GetAsync("/api/v1/notifications/unread-count");
    var count = await countResponse.Content.ReadFromJsonAsync<NotificationUnreadCountResponse>();
    Assert(count?.Count == 1, "Unread count must match unread inbox state.");

    var detailResponse = await client.GetAsync($"/api/v1/notifications/{firstId}");
    Assert(detailResponse.StatusCode == HttpStatusCode.OK, "Owned notification detail must return 200.");

    var foreignResponse = await client.GetAsync($"/api/v1/notifications/{foreignId}");
    Assert(foreignResponse.StatusCode == HttpStatusCode.NotFound, "Foreign notification must be hidden with 404.");

    var markReadResponse = await client.PostAsync($"/api/v1/notifications/{firstId}/read", null);
    Assert(markReadResponse.StatusCode == HttpStatusCode.OK, "Mark-as-read must return 200 for owned notification.");
    var marked = await markReadResponse.Content.ReadFromJsonAsync<InAppNotificationSnapshot>();
    Assert(marked is { IsRead: true } && marked.ReadAtUtc is not null, "Mark-as-read must persist read state.");
    var firstReadAt = marked!.ReadAtUtc;

    var repeatResponse = await client.PostAsync($"/api/v1/notifications/{firstId}/read", null);
    var repeated = await repeatResponse.Content.ReadFromJsonAsync<InAppNotificationSnapshot>();
    Assert(repeated?.ReadAtUtc == firstReadAt, "Repeated mark-as-read must be idempotent.");

    var finalCountResponse = await client.GetAsync("/api/v1/notifications/unread-count");
    var finalCount = await finalCountResponse.Content.ReadFromJsonAsync<NotificationUnreadCountResponse>();
    Assert(finalCount?.Count == 0, "Unread count must become zero after marking the final unread notification.");

    var missingRead = await client.PostAsync($"/api/v1/notifications/{Guid.NewGuid()}/read", null);
    Assert(missingRead.StatusCode == HttpStatusCode.NotFound, "Unknown notification mark-as-read must return 404.");

    Assert(secondId != Guid.Empty, "Read notification fixture must be persisted.");
    Console.WriteLine("AFW-BE-NOTIFICATION-1 protected paged in-app inbox HTTP scenarios: PASS");

    await app.DisposeAsync();
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

static HttpClient CreateClient(WebApplication app, Guid userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
    return client;
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class HeaderTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) || !Guid.TryParse(raw.ToString(), out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
