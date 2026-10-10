using System.Net;
using System.Text;
using System.Text.Json;
using MobileMoney.Production.Auth;

await RunAsync(
    "matching active AfWal session is accepted",
    async () =>
    {
        HttpRequestMessage? captured = null;
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using var client = Client(async request =>
        {
            captured = request;
            return Json(
                HttpStatusCode.OK,
                new
                {
                    userId,
                    sessionId,
                    deviceId = "device-1",
                    createdAtUtc =
                        DateTimeOffset.UtcNow.AddMinutes(-1),
                    expiresAtUtc =
                        DateTimeOffset.UtcNow.AddHours(1),
                    tokenVersion = 7L
                });
        });

        var validator =
            new HttpAfWalSessionValidator(client);

        var result = await validator.ValidateAsync(
            "access-token-value",
            userId,
            sessionId,
            7L);

        Assert(result.Succeeded, "Expected session validation success.");
        Assert(
            captured?.Headers.Authorization?.Scheme == "Bearer",
            "Expected Bearer authorization forwarding.");
        Assert(
            captured?.Headers.Authorization?.Parameter ==
                "access-token-value",
            "Expected the original access token to be forwarded.");
        Assert(
            captured?.RequestUri?.AbsolutePath ==
                "/api/v1/auth/session",
            "Expected the certified IdentityService session route.");
    });

await RunAsync(
    "stale token version is rejected",
    async () =>
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using var client = Client(_ =>
            Task.FromResult(
                Json(
                    HttpStatusCode.OK,
                    new
                    {
                        userId,
                        sessionId,
                        deviceId = "device-1",
                        createdAtUtc =
                            DateTimeOffset.UtcNow.AddMinutes(-1),
                        expiresAtUtc =
                            DateTimeOffset.UtcNow.AddHours(1),
                        tokenVersion = 8L
                    })));

        var result =
            await new HttpAfWalSessionValidator(client)
                .ValidateAsync(
                    "access-token-value",
                    userId,
                    sessionId,
                    7L);

        Assert(!result.Succeeded, "Expected stale token rejection.");
        Assert(
            result.ErrorCode ==
                AfWalSessionValidationErrorCode.TokenInvalid,
            "Expected AUTH_TOKEN_INVALID.");
    });

await RunAsync(
    "revoked remote session error is preserved",
    async () =>
    {
        using var client = Client(_ =>
            Task.FromResult(
                Json(
                    HttpStatusCode.Unauthorized,
                    new
                    {
                        code = "AUTH_SESSION_REVOKED",
                        message = "Session revoked.",
                        traceId = "trace-1"
                    })));

        var result =
            await new HttpAfWalSessionValidator(client)
                .ValidateAsync(
                    "access-token-value",
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1L);

        Assert(!result.Succeeded, "Expected revoked session rejection.");
        Assert(
            result.ErrorCode == "AUTH_SESSION_REVOKED",
            "Expected IdentityService error code preservation.");
    });

await RunAsync(
    "identity service failure is fail closed",
    async () =>
    {
        using var client = Client(_ =>
            Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.ServiceUnavailable)));

        var result =
            await new HttpAfWalSessionValidator(client)
                .ValidateAsync(
                    "access-token-value",
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1L);

        Assert(!result.Succeeded, "Expected fail-closed validation.");
        Assert(
            result.ErrorCode ==
                AfWalSessionValidationErrorCode.SessionUnavailable,
            "Expected session validation unavailable code.");
    });

await RunAsync(
    "expired session payload is rejected defensively",
    async () =>
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using var client = Client(_ =>
            Task.FromResult(
                Json(
                    HttpStatusCode.OK,
                    new
                    {
                        userId,
                        sessionId,
                        deviceId = "device-1",
                        createdAtUtc =
                            DateTimeOffset.UtcNow.AddHours(-2),
                        expiresAtUtc =
                            DateTimeOffset.UtcNow.AddMinutes(-1),
                        tokenVersion = 1L
                    })));

        var result =
            await new HttpAfWalSessionValidator(client)
                .ValidateAsync(
                    "access-token-value",
                    userId,
                    sessionId,
                    1L);

        Assert(!result.Succeeded, "Expected expired session rejection.");
        Assert(
            result.ErrorCode ==
                AfWalSessionValidationErrorCode.SessionExpired,
            "Expected AUTH_SESSION_EXPIRED.");
    });

Console.WriteLine(
    "AfWal session validation foundation scenarios passed.");

static HttpClient Client(
    Func<
        HttpRequestMessage,
        Task<HttpResponseMessage>> responder)
{
    return new HttpClient(
        new StubHttpMessageHandler(
            (request, _) => responder(request)))
    {
        BaseAddress =
            new Uri("https://identity.afrikawallet.test"),
        Timeout = TimeSpan.FromSeconds(5)
    };
}

static HttpResponseMessage Json(
    HttpStatusCode statusCode,
    object payload) =>
    new(statusCode)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(payload),
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

sealed class StubHttpMessageHandler(
    Func<
        HttpRequestMessage,
        CancellationToken,
        Task<HttpResponseMessage>> responder)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        responder(request, cancellationToken);
}
