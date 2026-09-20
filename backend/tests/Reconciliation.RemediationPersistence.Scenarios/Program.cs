using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Remediation;
using Reconciliation.Domain.Remediation;
using Reconciliation.Infrastructure.Remediation;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<ReconciliationCorrectiveActionDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new ReconciliationCorrectiveActionDbContext(options);
await db.Database.EnsureCreatedAsync();

var repository = new EfReconciliationCorrectiveActionRepository(db);
var resolutionId = Guid.NewGuid();
var reviewId = Guid.NewGuid();
var createdAt = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

var action = ReconciliationCorrectiveAction.Create(
    resolutionId,
    reviewId,
    "partner-a",
    "internal-1",
    "external-1",
    "SOURCE_RECORD.FIX",
    "Correct the source record.",
    "operator-1",
    createdAt);

await repository.AddAsync(action);

var loaded = await repository.GetByResolutionIdAsync(resolutionId);
Assert(loaded is not null, "Persisted corrective action must be readable.");
Assert(loaded!.ActionId == action.ActionId, "Action id must round-trip.");
Assert(loaded.Status == ReconciliationCorrectiveActionStatus.Pending, "New persisted action must remain pending.");
Assert(loaded.ActionCode == "SOURCE_RECORD.FIX", "Action code must round-trip.");

var createdAudit = await repository.ListByResolutionIdAsync(resolutionId);
Assert(createdAudit.Count == 1, "Create must append exactly one audit event.");
Assert(createdAudit[0].Event == ReconciliationCorrectiveActionAuditEvent.Created, "First audit event must be Created.");
Assert(createdAudit[0].ActorId == "operator-1", "Create actor must be audited.");

loaded.Complete("operator-2", "evidence://corrective/complete/1", createdAt.AddMinutes(5));
await repository.UpdateAsync(loaded);

var completed = await repository.GetByResolutionIdAsync(resolutionId);
Assert(completed?.Status == ReconciliationCorrectiveActionStatus.Completed, "Completed lifecycle must persist.");
Assert(completed?.CompletedBy == "operator-2", "Completion actor must persist.");
Assert(completed?.CompletionEvidenceReference == "evidence://corrective/complete/1", "Completion evidence must persist.");
Assert(completed?.CompletedAtUtc == createdAt.AddMinutes(5), "Completion timestamp must persist.");

var completedAudit = await repository.ListByResolutionIdAsync(resolutionId);
Assert(completedAudit.Count == 2, "Completion must append a second audit event.");
Assert(completedAudit[1].Event == ReconciliationCorrectiveActionAuditEvent.Completed, "Second audit event must be Completed.");
Assert(completedAudit[1].EvidenceReference == "evidence://corrective/complete/1", "Completion evidence must be audited.");
Assert(completedAudit[0].AuditId != completedAudit[1].AuditId, "Audit entries must be append-only and distinct.");

var cancelResolutionId = Guid.NewGuid();
var cancellable = ReconciliationCorrectiveAction.Create(
    cancelResolutionId,
    Guid.NewGuid(),
    "partner-a",
    "internal-2",
    null,
    "SOURCE_RECORD.REVIEW",
    "Cancel scenario.",
    "operator-1",
    createdAt);

await repository.AddAsync(cancellable);
cancellable.Cancel("operator-3", "Correction superseded by upstream fix.", createdAt.AddMinutes(3));
await repository.UpdateAsync(cancellable);

var cancelled = await repository.GetByResolutionIdAsync(cancelResolutionId);
Assert(cancelled?.Status == ReconciliationCorrectiveActionStatus.Cancelled, "Cancelled lifecycle must persist.");
Assert(cancelled?.CancelledBy == "operator-3", "Cancellation actor must persist.");
Assert(cancelled?.CancellationReason == "Correction superseded by upstream fix.", "Cancellation reason must persist.");

var cancelledAudit = await repository.ListByResolutionIdAsync(cancelResolutionId);
Assert(cancelledAudit.Count == 2, "Cancellation must append a second audit event.");
Assert(cancelledAudit[1].Event == ReconciliationCorrectiveActionAuditEvent.Cancelled, "Second audit event must be Cancelled.");
Assert(cancelledAudit[1].CancellationReason == "Correction superseded by upstream fix.", "Cancellation reason must be audited.");

try
{
    var duplicate = ReconciliationCorrectiveAction.Create(
        resolutionId,
        Guid.NewGuid(),
        "partner-a",
        "internal-3",
        null,
        "SOURCE_RECORD.RETRY",
        "Duplicate resolution scenario.",
        "operator-4",
        createdAt.AddMinutes(1));

    await repository.AddAsync(duplicate);
    throw new InvalidOperationException("Expected durable ResolutionId uniqueness conflict.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("already has a durable", StringComparison.Ordinal))
{
}

var unknown = await repository.GetByResolutionIdAsync(Guid.NewGuid());
Assert(unknown is null, "Unknown resolution must not resolve to a corrective action.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await repository.ListByResolutionIdAsync(resolutionId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException)
{
}

Assert(await db.Audit.CountAsync(x => x.ResolutionId == resolutionId) == 2, "Audit history must remain append-only.");

Console.WriteLine("AFW-BE-RECONCILIATION-REMEDIATION-1 persistence and durable audit scenarios: PASS");
