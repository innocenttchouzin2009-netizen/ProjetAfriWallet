using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;
using AfriWallet.Compliance.IdentityVerification.Application.Sessions;
using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;
using AfriWallet.Compliance.IdentityVerification.Infrastructure;
using AfriWallet.Compliance.IdentityVerification.Infrastructure.Providers;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-40} FAIL");
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    Console.WriteLine($"{name,-40} PASS");
}

var sessions = new InMemoryVerificationSessionRepository();
var audit = new InMemoryVerificationAuditStore();
var clock = new SystemVerificationClock();
var providerRegistry = new VerificationProviderRegistry(new IVerificationProvider[]
{
    new SandboxVerificationProvider("SANDBOX_DOC", "Sandbox Document Provider", VerificationType.Document),
    new SandboxVerificationProvider("SANDBOX_SELFIE", "Sandbox Selfie Provider", VerificationType.Selfie),
    new SandboxVerificationProvider("SANDBOX_LIVENESS", "Sandbox Liveness Provider", VerificationType.Liveness)
});
var service = new IdentityVerificationService(sessions, providerRegistry, audit, clock);

var created = await service.CreateAsync(
    new CreateVerificationCommand(
        Guid.NewGuid(),
        VerificationType.Document,
        "SANDBOX_DOC",
        "scenario-document-1",
        "scenario-runner"),
    CancellationToken.None);

Check("verification session created", created.Id != Guid.Empty);
Check("session starts in Created", created.Status == VerificationStatus.Created);

var idempotent = await service.CreateAsync(
    new CreateVerificationCommand(
        created.ComplianceProfileId,
        VerificationType.Document,
        "SANDBOX_DOC",
        "scenario-document-1",
        "scenario-runner"),
    CancellationToken.None);

Check("idempotent create returns same session", idempotent.Id == created.Id);

var submitted = await service.SubmitAsync(created.Id, "scenario-runner", CancellationToken.None);
Check("sandbox submission accepted", submitted.Status == VerificationStatus.Submitted);
Check("provider reference generated", !string.IsNullOrWhiteSpace(submitted.ProviderReference));

var processing = await service.StartProcessingAsync(created.Id, "scenario-runner", CancellationToken.None);
Check("processing started", processing.Status == VerificationStatus.Processing);

var completed = await service.CompleteAsync(
    new CompleteVerificationCommand(
        created.Id,
        true,
        "IDENTITY_VERIFIED",
        processing.ProviderReference!,
        "scenario-runner"),
    CancellationToken.None);

Check("verification completed", completed.Status == VerificationStatus.Verified);

var events = await audit.GetBySessionAsync(created.Id, CancellationToken.None);
Check("audit trail recorded", events.Count >= 4);

var unknownProviderBlocked = false;
try
{
    await service.CreateAsync(
        new CreateVerificationCommand(
            Guid.NewGuid(),
            VerificationType.Document,
            "UNKNOWN",
            "scenario-unknown",
            "scenario-runner"),
        CancellationToken.None);
}
catch (KeyNotFoundException)
{
    unknownProviderBlocked = true;
}

Check("unknown provider blocked", unknownProviderBlocked);

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0016.2 identity verification scenarios passed.");
Console.WriteLine("Providers: SANDBOX ONLY");
Console.WriteLine("External KYC certification: NOT CLAIMED");
Console.WriteLine("Decision: READY FOR REVIEW");
