using System.Security.Cryptography;
using System.Text;
using AfriWallet.PaymentPlatform.ProviderIntegration.Application;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Credentials;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Health;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Providers;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Secrets;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Webhooks;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-38} FAIL");
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    Console.WriteLine($"{name,-38} PASS");
}

static ProviderExecutionRequest Request(
    string operation,
    string correlationId)
    => new(
        "MTN",
        operation,
        correlationId,
        new Dictionary<string, string>());

var health = new InMemoryProviderHealthService();
var service = new ProviderIntegrationService(
    new SandboxCredentialService(),
    new SandboxProviderExecutor(),
    health,
    new RetryPolicy());

var success = await service.ExecuteAsync(Request("PAYMENT", "corr-001"), 2);

Check("provider execution", success.Success);
Check("provider reference", !string.IsNullOrWhiteSpace(success.ProviderReference));

var finalFailure = await service.ExecuteAsync(Request("FAIL_FINAL", "corr-002"), 2);

Check(
    "non-retryable failure",
    !finalFailure.Success && !finalFailure.Retryable);

var providerHealth = health.Get("MTN");

Check(
    "provider health",
    providerHealth.Available &&
    providerHealth.SuccessRate == 0.5 &&
    providerHealth.AverageLatencyMs >= 0);

var retryExecutor = new RetryThenSuccessExecutor(failuresBeforeSuccess: 2);
var retryService = new ProviderIntegrationService(
    new SandboxCredentialService(),
    retryExecutor,
    new InMemoryProviderHealthService(),
    new RetryPolicy());

var retryResult = await retryService.ExecuteAsync(Request("PAYMENT", "corr-003"), 2);

Check(
    "retry policy",
    retryResult.Success && retryExecutor.ExecutionCount == 3);

var exceptionExecutor = new ExceptionThenSuccessExecutor();
var exceptionService = new ProviderIntegrationService(
    new SandboxCredentialService(),
    exceptionExecutor,
    new InMemoryProviderHealthService(),
    new RetryPolicy());

var exceptionResult = await exceptionService.ExecuteAsync(
    Request("PAYMENT", "corr-004"),
    1);

Check(
    "exception retry",
    exceptionResult.Success && exceptionExecutor.ExecutionCount == 2);

var failingExecutor = new AlwaysFailExecutor();
var circuitService = new ProviderIntegrationService(
    new SandboxCredentialService(),
    failingExecutor,
    new InMemoryProviderHealthService(),
    new RetryPolicy());

for (var attempt = 0; attempt < 5; attempt++)
{
    var failure = await circuitService.ExecuteAsync(
        Request("PAYMENT", $"corr-circuit-{attempt}"),
        0);

    Check($"circuit failure {attempt + 1}", !failure.Success);
}

var circuitOpen = await circuitService.ExecuteAsync(
    Request("PAYMENT", "corr-circuit-open"),
    0);

Check(
    "circuit breaker foundation",
    circuitOpen.ErrorCode == "circuit_open" && failingExecutor.ExecutionCount == 5);

var scenarioSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
const string secretKey = "AFW_PROVIDER_MTN_WEBHOOK_SECRET";
const string payload = """{"status":"SUCCESS"}""";

Environment.SetEnvironmentVariable(secretKey, scenarioSecret);

try
{
    var verifier = new HmacWebhookVerifier(new EnvironmentSecretSource());

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(scenarioSecret));
    var signature = Convert.ToHexString(
        hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

    Check(
        "webhook signature",
        verifier.Verify(new ProviderWebhookVerificationRequest(
            "MTN",
            payload,
            signature)));

    Check(
        "invalid webhook rejected",
        !verifier.Verify(new ProviderWebhookVerificationRequest(
            "MTN",
            payload,
            "invalid")));
}
finally
{
    Environment.SetEnvironmentVariable(secretKey, null);
}

var sandboxCredential = await new SandboxCredentialService()
    .GetCredentialAsync("MTN");

Check(
    "sandbox credential",
    sandboxCredential.AccessToken.StartsWith(
        "sandbox-token-",
        StringComparison.Ordinal) &&
    sandboxCredential.ExpiresAt > DateTimeOffset.UtcNow);

Check(
    "audit foundation",
    service.AuditEvents.Count == 2 &&
    service.AuditEvents.All(item => item.ProviderCode == "MTN"));

Check(
    "telemetry foundation",
    service.TelemetryEvents.Count == 2 &&
    service.TelemetryEvents.All(item => item.DurationMs >= 0));

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0014.6 provider integration scenarios passed.");

internal sealed class RetryThenSuccessExecutor : IProviderExecutor
{
    private readonly int _failuresBeforeSuccess;
    private int _executionCount;

    public RetryThenSuccessExecutor(int failuresBeforeSuccess)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public int ExecutionCount => _executionCount;

    public Task<ProviderExecutionResult> ExecuteAsync(
        ProviderExecutionRequest request,
        ProviderCredential credential,
        CancellationToken cancellationToken = default)
    {
        var execution = Interlocked.Increment(ref _executionCount);

        return Task.FromResult(execution <= _failuresBeforeSuccess
            ? new ProviderExecutionResult(
                false,
                null,
                "temporary_failure",
                "Temporary scenario failure.",
                true)
            : new ProviderExecutionResult(
                true,
                "retry-provider-reference",
                null,
                null,
                false));
    }
}

internal sealed class ExceptionThenSuccessExecutor : IProviderExecutor
{
    private int _executionCount;

    public int ExecutionCount => _executionCount;

    public Task<ProviderExecutionResult> ExecuteAsync(
        ProviderExecutionRequest request,
        ProviderCredential credential,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _executionCount) == 1)
            throw new InvalidOperationException("Transient scenario exception.");

        return Task.FromResult(new ProviderExecutionResult(
            true,
            "exception-retry-reference",
            null,
            null,
            false));
    }
}

internal sealed class AlwaysFailExecutor : IProviderExecutor
{
    private int _executionCount;

    public int ExecutionCount => _executionCount;

    public Task<ProviderExecutionResult> ExecuteAsync(
        ProviderExecutionRequest request,
        ProviderCredential credential,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _executionCount);

        return Task.FromResult(new ProviderExecutionResult(
            false,
            null,
            "provider_unavailable",
            "Provider is unavailable in this scenario.",
            true));
    }
}