using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
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

    Assert(builder.Services.Any(x => x.ServiceType == typeof(IPaymentRequestEventPublisher) && x.ImplementationType == typeof(InAppPaymentRequestEventPublisher)),
        "Composition must wire the in-app payment request event publisher.");

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

    var listResponse = await client.GetAsync("/api/v1/notifications?limit=50");
    Assert(listResponse.StatusCode == HttpStatusCode.OK, "Authenticated inbox list must return 200.");
    var list = await listResponse.Content.ReadFromJsonAsync<List<InAppNotificationSnapshot>>();
    Assert(list is { Count: 2 }, "Inbox must contain only notifications owned by the authenticated user.");
    Assert(list![0].CreatedAtUtc >= list[1].CreatedAtUtc, "Inbox must be ordered newest first.");

    var unreadResponse = await client.GetAsync("/api/v1/notifications?unreadOnly=true&limit=50");
    var unread = await unreadResponse.Content.ReadFromJsonAsync<List<InAppNotificationSnapshot>>();
    Assert(unread is { Count: 1 } && unread[0].Id == firstId, "Unread filter must return only unread notifications.");

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
    Console.WriteLine("AFW-BE-NOTIFICATION-1 protected in-app inbox HTTP and read-state scenarios: PASS");

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
