using System.Security.Cryptography;
using System.Text;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Services;
using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;
using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Transfers;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Adapters;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Registries;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Repositories;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Resilience;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Security;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Telemetry;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-34} FAIL");
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    Console.WriteLine($"{name,-34} PASS");
}

var registry = new InMemoryBankProviderRegistry();
var repository = new InMemoryProviderTransferRepository();
var telemetry = new NoOpProviderTelemetry();
var service = new BankProviderIntegrationService(registry, repository, telemetry);

var executionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var request = new SubmitProviderTransferRequest(
    executionId,
    "SEPA-SANDBOX",
    "SEPA",
    2500,
    "USD",
    "sandbox-001");

var submitted = await service.SubmitAsync(request, CancellationToken.None);
Check("sandbox submission accepted", submitted.Status == ProviderTransferStatus.Submitted && !string.IsNullOrWhiteSpace(submitted.ProviderReference));
var roundTrip = await service.GetAsync(submitted.ProviderTransferId, CancellationToken.None);
Check("transfer persisted", roundTrip is not null && roundTrip.Status == ProviderTransferStatus.Submitted);

var invalidProvider = () => service.SubmitAsync(
    request with { ProviderCode = "PROD-BANK", IdempotencyKey = "sandbox-002" },
    CancellationToken.None);
Check("sandbox-only guard", await AssertThrowsAsync<KeyNotFoundException>(invalidProvider));

var signer = new HmacRequestSigner();
var verifier = new HmacWebhookVerifier(signer);
var secret = "sandbox-provider-secret";
var payload = "{\"status\":\"SUCCESS\"}";
var signature = signer.Sign(payload, secret);
var webhookResult = new BankWebhookService(registry, verifier, telemetry)
    .Process(new ProviderWebhookRequest("SEPA-SANDBOX", payload, signature), secret);
Check("webhook verified", webhookResult.Accepted && webhookResult.EventType == "sandbox.transfer.updated");

var retryExecutor = new ProviderRetryExecutor();
var retryAttempts = 0;
var retryResult = await retryExecutor.ExecuteAsync(
    async _ =>
    {
        retryAttempts++;
        if (retryAttempts < 3)
            throw new InvalidOperationException("transient");

        return "OK";
    },
    value => value == "OK",
    3,
    CancellationToken.None);
Check("retry executor recovers", retryResult == "OK" && retryAttempts == 3);

var circuit = new ProviderCircuitBreaker();
Check("circuit initially open to execution", circuit.CanExecute());
for (var i = 0; i < 3; i++)
{
    circuit.RecordFailure();
}
Check("circuit closed after threshold", !circuit.CanExecute());
circuit.RecordSuccess();
Check("circuit recovers after success", circuit.CanExecute());

var provider = registry.GetRequired("SEPA-SANDBOX");
Check("provider definition sandbox", provider.Environment == BankProviderEnvironment.Sandbox && provider.Supports(BankProviderCapability.Webhooks));
var health = await new SandboxSepaBankAdapter().CheckHealthAsync(CancellationToken.None);
Check("sandbox adapter health", health.Healthy && health.ProviderCode == "SEPA-SANDBOX");

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0015.6 provider integration scenarios passed.");

static async Task<bool> AssertThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
        return false;
    }
    catch (TException)
    {
        return true;
    }
}

