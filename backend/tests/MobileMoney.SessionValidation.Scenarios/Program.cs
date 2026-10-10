using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using MobileMoney.Production.Authentication;

await RunAsync(
    "valid Identity session is accepted and bearer token is forwarded",
    async () =>
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert(
                request.Headers.Authorization?.Scheme == "Bearer",
                "Bearer scheme must be forwarded.");
            Assert(
                request.Headers.Authorization?.Parameter == "access-token",
                "Bearer token must be forwarded unchanged.");

            return JsonResponse(
                HttpStatusCode.OK,
                $$"""
                {
                  "userId": "{{userId}}",
                  "sessionId": "{{sessionId}}",
                  "deviceId": "device-1",
                  "createdAtUtc": "2026-10-10T20:00:00Z",
                  "expiresAtUtc": "2099-10-10T20:30:00Z",
                  "tokenVersion": 7
                }
                """);
        });

        var validator = CreateValidator(handler);
        var result = await validator.ValidateAsync("Bearer access-token");

        Assert(result.Succeeded, "Valid session must succeed.");
        Assert(result.Session?.UserId == userId, "User id mismatch.");
        Assert(result.Session?.SessionId == sessionId, "Session id mismatch.");
        Assert(result.Session?.TokenVersion == 7, "Token version mismatch.");
    });

await RunAsync(
    "missing bearer token fails closed",
    async () =>
    {
        var validator = CreateValidator(
            new StubHttpMessageHandler(_ =>
                throw new InvalidOperationException(
                    "Identity must not be called without a bearer token.")));

        var result = await validator.ValidateAsync(null);

        Assert(
            result.Status == AfWalSessionValidationStatus.MissingToken,
            "Missing token must be rejected.");
    });

await RunAsync(
    "revoked or invalid Identity session fails closed",
    async () =>
    {
        var validator = CreateValidator(
            new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var result = await validator.ValidateAsync("Bearer access-token");

        Assert(
            result.Status == AfWalSessionValidationStatus.Invalid,
            "401 from Identity must invalidate the session.");
    });

await RunAsync(
    "expired session payload fails closed",
    async () =>
    {
        var validator = CreateValidator(
            new StubHttpMessageHandler(_ =>
                JsonResponse(
                    HttpStatusCode.OK,
                    $$"""
                    {
                      "userId": "{{Guid.NewGuid()}}",
                      "sessionId": "{{Guid.NewGuid()}}",
                      "deviceId": "device-expired",
                      "expiresAtUtc": "2020-01-01T00:00:00Z",
                      "tokenVersion": 2
                    }
                    """)));

        var result = await validator.ValidateAsync("Bearer access-token");

        Assert(
            result.Status == AfWalSessionValidationStatus.Expired,
            "Expired session must be rejected.");
    });

await RunAsync(
    "malformed Identity session contract fails closed",
    async () =>
    {
        var validator = CreateValidator(
            new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, "{not-json")));

        var result = await validator.ValidateAsync("Bearer access-token");

        Assert(
            result.Status == AfWalSessionValidationStatus.Incompatible,
            "Malformed session contract must be rejected.");
    });

await RunAsync(
    "Identity service failure fails closed",
    async () =>
    {
        var validator = CreateValidator(
            new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(
                    HttpStatusCode.ServiceUnavailable)));

        var result = await validator.ValidateAsync("Bearer access-token");

        Assert(
            result.Status == AfWalSessionValidationStatus.Unavailable,
            "Identity 5xx must deny the financial operation.");
    });

await RunAsync(
    "Identity transport failure fails closed",
    async () =>
    {
        var validator = CreateValidator(
            new ThrowingHttpMessageHandler(
                new HttpRequestException("identity unavailable")));

        var result = await validator.ValidateAsync("Bearer access-token");

        Assert(
            result.Status == AfWalSessionValidationStatus.Unavailable,
            "Transport failure must deny the financial operation.");
    });

await RunAsync(
    "session guard stores valid session and allows continuation",
    async () =>
    {
        var session = new AfWalValidatedSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "device-guard",
            DateTimeOffset.UtcNow.AddMinutes(10),
            3);

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer access-token";

        var denied = await MobileMoneySessionGuardExtensions.AuthorizeAsync(
            context,
            new FixedValidator(
                AfWalSessionValidationResult.Valid(session)));

        Assert(denied is null, "Valid session must continue.");
        Assert(
            ReferenceEquals(
                context.Items[AfWalValidatedSession.HttpContextItemKey],
                session),
            "Validated session must be attached to HttpContext.");
    });

await RunAsync(
    "session guard returns 401 for revoked or invalid session",
    async () =>
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer access-token";

        var denied = await MobileMoneySessionGuardExtensions.AuthorizeAsync(
            context,
            new FixedValidator(
                AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Invalid)));

        Assert(denied is IStatusCodeHttpResult, "Denied result must expose status.");
        Assert(
            ((IStatusCodeHttpResult)denied!).StatusCode ==
            StatusCodes.Status401Unauthorized,
            "Invalid session must return 401.");
    });

await RunAsync(
    "session guard returns 503 when validation is unavailable",
    async () =>
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer access-token";

        var denied = await MobileMoneySessionGuardExtensions.AuthorizeAsync(
            context,
            new FixedValidator(
                AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Unavailable)));

        Assert(denied is IStatusCodeHttpResult, "Denied result must expose status.");
        Assert(
            ((IStatusCodeHttpResult)denied!).StatusCode ==
            StatusCodes.Status503ServiceUnavailable,
            "Unavailable validation must return 503.");
    });

Console.WriteLine(
    "Mobile Money AfWal session validation scenarios passed.");

static HttpAfWalSessionValidator CreateValidator(
    HttpMessageHandler handler)
{
    var client = new HttpClient(handler);
    var options = new AfWalSessionValidationOptions(
        new Uri("https://identity.afrikawallet.test"),
        TimeSpan.FromSeconds(2));

    return new HttpAfWalSessionValidator(client, options);
}

static HttpResponseMessage JsonResponse(
    HttpStatusCode statusCode,
    string json) =>
    new(statusCode)
    {
        Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json")
    };

static async Task RunAsync(
    string name,
    Func<Task> action)
{
    await action();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(
    bool condition,
    string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class FixedValidator(
    AfWalSessionValidationResult result)
    : IAfWalSessionValidator
{
    public Task<AfWalSessionValidationResult> ValidateAsync(
        string? authorizationHeader,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(result);
}

sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(handler(request));
}

sealed class ThrowingHttpMessageHandler(
    Exception exception)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromException<HttpResponseMessage>(exception);
}
