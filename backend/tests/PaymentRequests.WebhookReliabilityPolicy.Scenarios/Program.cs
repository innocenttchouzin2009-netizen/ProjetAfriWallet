using System.Net;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks;
using AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static PaymentRequestWebhookDeliveryAttempt Attempt(
    Guid subscriptionId,
    PaymentRequestWebhookDeliveryAttemptOutcome outcome,
    DateTimeOffset atUtc,
    int? httpStatusCode = null) =>
    PaymentRequestWebhookDeliveryAttempt.Create(
        subscriptionId,
        Guid.NewGuid(),
        outcome,
        httpStatusCode,
        10,
        atUtc,
        atUtc.AddMilliseconds(10));

var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-webhook-policy-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<PaymentRequestWebhookSubscriptionDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

const string secretReference = "AFW_TEST_WEBHOOK_POLICY_SECRET";
const string rawSecret = "0123456789abcdef0123456789abcdef";
Environment.SetEnvironmentVariable(secretReference, rawSecret);

try
{
    await using var db = new PaymentRequestWebhookSubscriptionDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var registry = new EfPaymentRequestWebhookSubscriptionRegistry(db);
    var attempts = new EfPaymentRequestWebhookDeliveryAttemptStore(db);
    var audit = new EfPaymentRequestWebhookSubscriptionAuditStore(db);
    var policyOptions = PaymentRequestWebhookReliabilityProtectionOptions.Default;
    var now = new DateTimeOffset(2026, 9, 18, 17, 30, 0, TimeSpan.Zero);
    var protector = new PaymentRequestWebhookReliabilityProtector(
        registry,
        attempts,
        audit,
        policyOptions,
        new FixedTimeProvider(now.AddMinutes(10)));

    var sustained = PaymentRequestWebhookSubscription.Create(
        "integration.sustained",
        null,
        new Uri("https://sustained.example.test/hooks"),
        "policy-key-1",
        secretReference,
        ["payment-request.paid"],
        now);
    await registry.AddAsync(sustained);

    for (var index = 0; index < 4; index++)
    {
        await attempts.AppendAsync(Attempt(
            sustained.Id,
            PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure,
            now.AddSeconds(index),
            503));
        var beforeThreshold = await protector.EvaluateAndProtectAsync(sustained.Id);
        Assert(beforeThreshold.Status == PaymentRequestWebhookReliabilityProtectionStatus.NoAction,
            "Subscription must remain active before minimum/consecutive thresholds are reached.");
    }

    await attempts.AppendAsync(Attempt(
        sustained.Id,
        PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure,
        now.AddSeconds(4),
        503));
    var protectedResult = await protector.EvaluateAndProtectAsync(sustained.Id);
    Assert(protectedResult.Status == PaymentRequestWebhookReliabilityProtectionStatus.AutoDisabled,
        "Fifth consecutive failed attempt at 100% failure rate must auto-disable the subscription.");
    Assert(protectedResult.ConsecutiveFailureCount == 5, "Consecutive failure count mismatch.");

    var sustainedReloaded = await registry.GetAsync(sustained.Id);
    Assert(sustainedReloaded?.Status == PaymentRequestWebhookSubscriptionStatus.Disabled,
        "Protected subscription must be persisted as disabled.");

    var sustainedAudit = await audit.ListAsync(sustained.Id);
    var policyAudit = sustainedAudit.Where(x =>
        x.Operation == PaymentRequestWebhookSubscriptionAuditOperation.ReliabilityAutoDisabled).ToArray();
    Assert(policyAudit.Length == 1, "Automatic reliability protection must write exactly one audit record.");
    Assert(policyAudit[0].ActorSubject == "system:webhook-reliability-policy",
        "Automatic protection audit actor mismatch.");
    Assert(policyAudit[0].Detail?.Contains("minAttempts=5", StringComparison.Ordinal) == true &&
           policyAudit[0].Detail?.Contains("failureRate=0.8", StringComparison.Ordinal) == true &&
           policyAudit[0].Detail?.Contains("consecutiveFailures=5", StringComparison.Ordinal) == true,
        "Automatic protection audit must contain explicit thresholds.");

    var alreadyProtected = await protector.EvaluateAndProtectAsync(sustained.Id);
    Assert(alreadyProtected.Status == PaymentRequestWebhookReliabilityProtectionStatus.AlreadyDisabled,
        "Disabled subscription must not be disabled again.");
    Assert((await audit.ListAsync(sustained.Id)).Count(x =>
        x.Operation == PaymentRequestWebhookSubscriptionAuditOperation.ReliabilityAutoDisabled) == 1,
        "Repeated evaluation must not duplicate automatic protection audit.");

    var recovered = PaymentRequestWebhookSubscription.Create(
        "integration.recovered",
        null,
        new Uri("https://recovered.example.test/hooks"),
        "policy-key-2",
        secretReference,
        ["payment-request.cancelled"],
        now);
    await registry.AddAsync(recovered);

    for (var index = 0; index < 4; index++)
        await attempts.AppendAsync(Attempt(
            recovered.Id,
            PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure,
            now.AddMinutes(1).AddSeconds(index),
            503));
    await attempts.AppendAsync(Attempt(
        recovered.Id,
        PaymentRequestWebhookDeliveryAttemptOutcome.Success,
        now.AddMinutes(1).AddSeconds(4),
        202));

    var recoveryResult = await protector.EvaluateAndProtectAsync(recovered.Id);
    Assert(recoveryResult.Status == PaymentRequestWebhookReliabilityProtectionStatus.NoAction,
        "A recent successful delivery must break the consecutive failure threshold.");
    Assert((await registry.GetAsync(recovered.Id))?.Status == PaymentRequestWebhookSubscriptionStatus.Active,
        "Recovered subscription must remain active.");

    var permanent = PaymentRequestWebhookSubscription.Create(
        "integration.permanent",
        null,
        new Uri("https://permanent.example.test/hooks"),
        "policy-key-3",
        secretReference,
        ["payment-request.paid"],
        now);
    await registry.AddAsync(permanent);

    var permanentOutcomes = new[]
    {
        PaymentRequestWebhookDeliveryAttemptOutcome.Success,
        PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure,
        PaymentRequestWebhookDeliveryAttemptOutcome.Success,
        PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure,
        PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure
    };
    for (var index = 0; index < permanentOutcomes.Length; index++)
        await attempts.AppendAsync(Attempt(
            permanent.Id,
            permanentOutcomes[index],
            now.AddMinutes(2).AddSeconds(index),
            permanentOutcomes[index] == PaymentRequestWebhookDeliveryAttemptOutcome.Success ? 202 : 400));

    var permanentResult = await protector.EvaluateAndProtectAsync(permanent.Id);
    Assert(permanentResult.Status == PaymentRequestWebhookReliabilityProtectionStatus.AutoDisabled,
        "Three permanent failures after minimum attempts must auto-disable even below the failure-rate threshold.");
    Assert(permanentResult.Reason == "permanent-failure-threshold",
        "Permanent failure protection reason mismatch.");

    var transportSubscription = PaymentRequestWebhookSubscription.Create(
        "integration.transport",
        null,
        new Uri("https://transport.example.test/hooks"),
        "policy-key-4",
        secretReference,
        ["payment-request.paid"],
        now);
    await registry.AddAsync(transportSubscription);

    var handler = new CountingFailureHandler();
    var transport = new RegistryBackedHttpPaymentRequestEventTransport(
        new HttpClient(handler),
        registry,
        new EnvironmentPaymentRequestWebhookSigningSecretResolver(),
        attempts,
        protector,
        TimeProvider.System);

    for (var index = 0; index < 5; index++)
    {
        try
        {
            await transport.DispatchAsync(new PaymentRequestEventDispatch(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "payment-request.paid",
                now.AddMinutes(3).AddSeconds(index),
                "{\"status\":\"Paid\"}"));
        }
        catch (PaymentRequestEventTransportException exception)
            when (exception.FailureKind == PaymentRequestEventTransportFailureKind.Transient)
        {
        }
    }

    Assert(handler.CallCount == 5,
        "Reliability protection must not introduce any parallel retry; five dispatches must make exactly five HTTP calls.");
    Assert((await registry.GetAsync(transportSubscription.Id))?.Status ==
           PaymentRequestWebhookSubscriptionStatus.Disabled,
        "Repeated delivery failures must automatically protect the transport subscription.");

    await transport.DispatchAsync(new PaymentRequestEventDispatch(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "payment-request.paid",
        now.AddMinutes(4),
        "{\"status\":\"Paid\"}"));
    Assert(handler.CallCount == 5,
        "Disabled subscription must be excluded from future delivery fan-out.");

    Console.WriteLine("AFW-BE-REQUEST Commit 13 webhook reliability policy and automatic subscription protection scenarios: PASS");
}
finally
{
    Environment.SetEnvironmentVariable(secretReference, null);
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class CountingFailureHandler : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
