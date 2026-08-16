using AfriWallet.CompliancePlatform.ComplianceProfile.Application;
using AfriWallet.CompliancePlatform.ComplianceProfile.Application.Interfaces;
using AfriWallet.CompliancePlatform.ComplianceProfile.Infrastructure.Audit;
using AfriWallet.CompliancePlatform.ComplianceProfile.Infrastructure.Repositories;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-40} FAIL");
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    Console.WriteLine($"{name,-40} PASS");
}

var repository = new InMemoryComplianceProfileRepository();
var audit = new ComplianceAuditSink();
var service = new ComplianceProfileService(repository, audit);

var profile = await service.CreateAsync(
    new CreateComplianceProfileRequest("CUST-9001", "individual", "mobile_app", "low"),
    CancellationToken.None);

Check("profile creation", profile.ProfileId != Guid.Empty);
Check("profile status", profile.Status == "Draft");

var withDocument = await service.AddDocumentAsync(
    new AddDocumentRequest(profile.ProfileId, "passport", "P1234567", "CM"),
    CancellationToken.None);

Check("document added", withDocument.Documents.Count == 1);
Check("document status", withDocument.Documents.First().Status == "received");

var submitted = await service.ReviewAsync(
    new ReviewComplianceProfileRequest(profile.ProfileId, "reviewer-1", true, "document verified"),
    CancellationToken.None);

Check("approval review", submitted.Status == "Active");
Check("audit trail populated", submitted.AuditTrail.Count >= 3);

var profileList = await service.ListByCustomerAsync("CUST-9001", CancellationToken.None);
Check("customer listing", profileList.Count == 1);

var rejected = await service.CreateAsync(
    new CreateComplianceProfileRequest("CUST-9002", "merchant", "web_checkout"),
    CancellationToken.None);

var rejectedResult = await service.ReviewAsync(
    new ReviewComplianceProfileRequest(rejected.ProfileId, "reviewer-2", false, "identity mismatch"),
    CancellationToken.None);

Check("rejected status", rejectedResult.Status == "Rejected");

var suspended = await service.SuspendAsync(rejected.ProfileId, "ops-1", "manual review required", CancellationToken.None);
Check("suspension", suspended.Status == "Suspended");

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0016.1 compliance profile scenarios passed.");
