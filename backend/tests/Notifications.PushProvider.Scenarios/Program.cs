using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Push.Infrastructure;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 16, 12, 30, 0, TimeSpan.Zero);
var userId = Guid.NewGuid();
var message = PushNotificationMessage.Create(
    Guid.NewGuid(),
    userId,
    "Payment received",
    "Your payment request was paid.",
    "afwal://payment-requests/123",
    now);

var android = DevicePushRegistration.Create(userId, "android-1", PushDevicePlatform.Android, "fcm-token-1", now);
var ios = DevicePushRegistration.Create(userId, "ios-1", PushDevicePlatform.Ios, "apns-token-1", now.AddSeconds(1));
var options = new PushProviderOptions("afwal-prod", "com.afrikawallet.app", false);
var tokenSource = new FixedTokenSource("fcm-access", "apns-provider");

var fcmHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
{
    Content = new StringContent("{\"name\":\"projects/afwal-prod/messages/123\"}", Encoding.UTF8, "application/json")
});
var fcm = new FcmHttpV1PushProviderClient(new HttpClient(fcmHandler), options, tokenSource);
var fcmResult = await fcm.SendAsync(android, message);
Assert(fcmResult.Disposition == PushDeliveryDisposition.Delivered, "FCM success must be delivered.");
Assert(fcmResult.ProviderMessageId == "projects/afwal-prod/messages/123", "FCM message id must be returned.");
Assert(fcmHandler.LastUri?.ToString() == "https://fcm.googleapis.com/v1/projects/afwal-prod/messages:send", "FCM v1 endpoint mismatch.");
Assert(fcmHandler.LastAuthorization?.Scheme == "Bearer" && fcmHandler.LastAuthorization.Parameter == "fcm-access", "FCM bearer auth missing.");
Assert(fcmHandler.LastBody?.Contains("fcm-token-1", StringComparison.Ordinal) == true, "FCM device token missing from payload.");
Assert(fcmHandler.LastBody?.Contains("Payment received", StringComparison.Ordinal) == true, "FCM title missing from payload.");
Assert(fcmHandler.LastBody?.Contains("afwal://payment-requests/123", StringComparison.Ordinal) == true, "FCM deep link missing from payload.");

var fcmRetryHandler = new RecordingHandler(_ =>
{
    var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(20));
    return response;
});
var fcmRetry = await new FcmHttpV1PushProviderClient(new HttpClient(fcmRetryHandler), options, tokenSource)
    .SendAsync(android, message);
Assert(fcmRetry.Disposition == PushDeliveryDisposition.RetryableFailure, "FCM 503 must be retryable.");
Assert(fcmRetry.ErrorCode == "fcm-http-503", "FCM retry code mismatch.");
Assert(fcmRetry.RetryAfter == TimeSpan.FromSeconds(20), "FCM Retry-After must be preserved.");

var apnsHandler = new RecordingHandler(_ =>
{
    var response = new HttpResponseMessage(HttpStatusCode.OK);
    response.Headers.TryAddWithoutValidation("apns-id", "apns-message-123");
    return response;
});
var apns = new ApnsHttp2PushProviderClient(new HttpClient(apnsHandler), options, tokenSource);
var apnsResult = await apns.SendAsync(ios, message);
Assert(apnsResult.Disposition == PushDeliveryDisposition.Delivered, "APNs success must be delivered.");
Assert(apnsResult.ProviderMessageId == "apns-message-123", "APNs id must be returned.");
Assert(apnsHandler.LastUri?.ToString() == "https://api.push.apple.com/3/device/apns-token-1", "APNs endpoint mismatch.");
Assert(apnsHandler.LastVersion?.Major == 2, "APNs request must use HTTP/2.");
Assert(apnsHandler.LastAuthorization?.Scheme.Equals("bearer", StringComparison.OrdinalIgnoreCase) == true && apnsHandler.LastAuthorization.Parameter == "apns-provider", "APNs bearer auth missing.");
Assert(apnsHandler.LastHeaders.TryGetValue("apns-topic", out var topic) && topic == "com.afrikawallet.app", "APNs topic missing.");
Assert(apnsHandler.LastHeaders.TryGetValue("apns-push-type", out var pushType) && pushType == "alert", "APNs push type missing.");
Assert(apnsHandler.LastBody?.Contains("Payment received", StringComparison.Ordinal) == true, "APNs alert missing from payload.");

var apnsGone = await new ApnsHttp2PushProviderClient(
        new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Gone))),
        options,
        tokenSource)
    .SendAsync(ios, message);
Assert(apnsGone.Disposition == PushDeliveryDisposition.PermanentFailure, "APNs 410 must be permanent.");
Assert(apnsGone.ErrorCode == "apns-http-410", "APNs permanent code mismatch.");

var repository = new InMemoryRegistrationRepository([android, ios]);
var fcmRecording = new FixedProviderClient(PushProviderKind.Fcm, [PushDevicePlatform.Android, PushDevicePlatform.Web], PushDeliveryResult.Delivered("fcm-ok"));
var apnsRecording = new FixedProviderClient(PushProviderKind.Apns, [PushDevicePlatform.Ios], PushDeliveryResult.Retryable("apns-http-503"));
var transport = new ProviderBackedPushNotificationTransport(repository, [fcmRecording, apnsRecording]);
var aggregate = await transport.SendAsync(message);
Assert(aggregate.Disposition == PushDeliveryDisposition.Delivered, "One delivered device must make user-level delivery successful.");
Assert(fcmRecording.Calls == 1 && apnsRecording.Calls == 1, "All active devices must be attempted once.");
Assert(fcmRecording.LastToken == "fcm-token-1", "Android token must route to FCM.");
Assert(apnsRecording.LastToken == "apns-token-1", "iOS token must route to APNs.");

var retryTransport = new ProviderBackedPushNotificationTransport(
    repository,
    [
        new FixedProviderClient(PushProviderKind.Fcm, [PushDevicePlatform.Android, PushDevicePlatform.Web], PushDeliveryResult.Retryable("fcm-http-503")),
        new FixedProviderClient(PushProviderKind.Apns, [PushDevicePlatform.Ios], PushDeliveryResult.Permanent("apns-http-410"))
    ]);
var aggregateRetry = await retryTransport.SendAsync(message);
Assert(aggregateRetry.Disposition == PushDeliveryDisposition.RetryableFailure, "Retryable failure must prevail when nothing was delivered.");

var emptyTransport = new ProviderBackedPushNotificationTransport(new InMemoryRegistrationRepository([]), [fcmRecording, apnsRecording]);
var noDevice = await emptyTransport.SendAsync(message);
Assert(noDevice.Disposition == PushDeliveryDisposition.PermanentFailure && noDevice.ErrorCode == "no-active-push-device", "Missing devices must fail permanently.");

using var cancellation = new CancellationTokenSource();
cancellation.Cancel();
try
{
    await transport.SendAsync(message, cancellation.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-PUSH-NOTIFICATION-1 provider adapter and delivery orchestration scenarios: PASS");

sealed class FixedTokenSource(string fcm, string apns) : IPushProviderAuthorizationTokenSource
{
    public Task<string?> GetTokenAsync(PushProviderKind provider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(provider == PushProviderKind.Fcm ? fcm : apns);
    }
}

sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public Uri? LastUri { get; private set; }
    public AuthenticationHeaderValue? LastAuthorization { get; private set; }
    public Version? LastVersion { get; private set; }
    public string? LastBody { get; private set; }
    public Dictionary<string, string> LastHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastUri = request.RequestUri;
        LastAuthorization = request.Headers.Authorization;
        LastVersion = request.Version;
        LastHeaders.Clear();
        foreach (var header in request.Headers)
        {
            LastHeaders[header.Key] = string.Join(",", header.Value);
        }
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return responder(request);
    }
}

sealed class FixedProviderClient(
    PushProviderKind provider,
    IReadOnlyCollection<PushDevicePlatform> platforms,
    PushDeliveryResult result) : IPushProviderClient
{
    public PushProviderKind Provider => provider;
    public int Calls { get; private set; }
    public string? LastToken { get; private set; }

    public bool Supports(PushDevicePlatform platform) => platforms.Contains(platform);

    public Task<PushDeliveryResult> SendAsync(
        DevicePushRegistration registration,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastToken = registration.Token;
        return Task.FromResult(result);
    }
}

sealed class InMemoryRegistrationRepository(IEnumerable<DevicePushRegistration> registrations) : IDevicePushRegistrationRepository
{
    private readonly List<DevicePushRegistration> values = registrations.ToList();

    public Task<DevicePushRegistration?> GetActiveByDeviceAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.FirstOrDefault(value => value.UserId == userId && value.DeviceId == deviceId && value.IsActive));

    public Task<DevicePushRegistration?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.FirstOrDefault(value => value.TokenHash == tokenHash && value.IsActive));

    public Task<IReadOnlyList<DevicePushRegistration>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<DevicePushRegistration>>(values.Where(value => value.UserId == userId && value.IsActive).ToArray());
    }

    public Task AddAsync(DevicePushRegistration registration, CancellationToken cancellationToken = default)
    {
        values.Add(registration);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DevicePushRegistration registration, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
