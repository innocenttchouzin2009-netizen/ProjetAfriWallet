using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Application.Commands;
using AfriWallet.Merchants.Registry.Application.Services;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;
using AfriWallet.Merchants.Registry.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2027, 1, 12, 9, 0, 0, TimeSpan.Zero);
const string actor = "scenario-runner";

var repository = new InMemoryMerchantRepository();
var audit = new InMemoryMerchantAuditStore();
var service = new MerchantRegistryService(repository, audit, new FixedClock(now));

BusinessProfile MakeProfile(string legalName, string tradingName, string countryCode = "CI", string ownerSuffix = "") =>
    new(
        legalName,
        tradingName,
        MerchantType.Company,
        countryCode,
        "XOF",
        "Retail",
        "RC-001",
        "TAX-001",
        new BusinessAddress("12 Main Street", null, "Abidjan", "00225", countryCode),
        new MerchantContact($"contact{ownerSuffix}@example.com", "+225000000"));

// 1-3: creation gating.
var primaryOwner = "AWID-MERCHANT-001";
var primary = await service.CreateAsync(new CreateMerchantCommand(primaryOwner, MakeProfile("Primary Legal SARL", "Primary Store"), actor));
Check("merchant id has AFM prefix", primary.MerchantId.StartsWith("AFM-", StringComparison.Ordinal), ref passed);
Check("merchant starts in draft", primary.Status == MerchantStatus.Draft, ref passed);

var duplicateOwnerBlocked = false;
try
{
    await service.CreateAsync(new CreateMerchantCommand(primaryOwner, MakeProfile("Other Legal SARL", "Other Store"), actor));
}
catch (InvalidOperationException)
{
    duplicateOwnerBlocked = true;
}
Check("duplicate owner AWID blocked", duplicateOwnerBlocked, ref passed);

var duplicateLegalNameBlocked = false;
try
{
    await service.CreateAsync(new CreateMerchantCommand("AWID-MERCHANT-002", MakeProfile("Primary Legal SARL", "Different Trading"), actor));
}
catch (InvalidOperationException)
{
    duplicateLegalNameBlocked = true;
}
Check("duplicate legal name in same country blocked", duplicateLegalNameBlocked, ref passed);

// 4-9: lifecycle progression.
var registered = await service.RegisterAsync(new RegisterMerchantCommand(primary.MerchantId, actor));
Check("merchant registered", registered.Status == MerchantStatus.Registered, ref passed);

var reRegisterBlocked = false;
try
{
    await service.RegisterAsync(new RegisterMerchantCommand(primary.MerchantId, actor));
}
catch (InvalidOperationException)
{
    reRegisterBlocked = true;
}
Check("re-registration blocked", reRegisterBlocked, ref passed);

var pendingVerification = await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(primary.MerchantId, MerchantStatus.PendingVerification, actor));
Check("merchant enters pending verification", pendingVerification.Status == MerchantStatus.PendingVerification, ref passed);

var pendingFromDraftBlocked = false;
var freshOwner = "AWID-MERCHANT-003";
var fresh = await service.CreateAsync(new CreateMerchantCommand(freshOwner, MakeProfile("Fresh Legal SARL", "Fresh Store"), actor));
try
{
    await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(fresh.MerchantId, MerchantStatus.PendingVerification, actor));
}
catch (InvalidOperationException)
{
    pendingFromDraftBlocked = true;
}
Check("pending verification blocked from draft", pendingFromDraftBlocked, ref passed);

var active = await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(primary.MerchantId, MerchantStatus.Active, actor));
Check("merchant activated", active.Status == MerchantStatus.Active, ref passed);

var activeDirectFromDraftBlocked = false;
try
{
    await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(fresh.MerchantId, MerchantStatus.Active, actor));
}
catch (InvalidOperationException)
{
    activeDirectFromDraftBlocked = true;
}
Check("direct activation from draft blocked", activeDirectFromDraftBlocked, ref passed);

// 10-12: suspend/resume.
var suspended = await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(primary.MerchantId, MerchantStatus.Suspended, actor));
Check("merchant suspended", suspended.Status == MerchantStatus.Suspended, ref passed);

var suspendNotActiveBlocked = false;
try
{
    await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(fresh.MerchantId, MerchantStatus.Suspended, actor));
}
catch (InvalidOperationException)
{
    suspendNotActiveBlocked = true;
}
Check("suspend blocked when not active", suspendNotActiveBlocked, ref passed);

var resumed = await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(primary.MerchantId, MerchantStatus.Active, actor));
Check("merchant resumed", resumed.Status == MerchantStatus.Active, ref passed);

// 13: declared capabilities are deduplicated.
var withCapabilities = await service.SetCapabilitiesAsync(new SetMerchantCapabilitiesCommand(
    primary.MerchantId,
    new[] { MerchantCapability.OnlinePayments, MerchantCapability.QrPayments, MerchantCapability.OnlinePayments },
    actor));
Check("capabilities deduplicated", withCapabilities.Capabilities.Count == 2, ref passed);

// 14: profile update.
var updatedProfile = MakeProfile("Primary Legal SARL", "Primary Store Renamed");
var withUpdatedProfile = await service.UpdateProfileAsync(new UpdateMerchantProfileCommand(primary.MerchantId, updatedProfile, actor));
Check("profile updated", withUpdatedProfile.Profile.TradingName == "Primary Store Renamed", ref passed);

// 15-17: closure and terminal-state immutability.
var closed = await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(primary.MerchantId, MerchantStatus.Closed, actor));
Check("merchant closed", closed.Status == MerchantStatus.Closed, ref passed);

var closedRecord = await repository.GetAsync(new MerchantId(primary.MerchantId));
Check("closed timestamp recorded", closedRecord is not null && closedRecord.ClosedAtUtc == now, ref passed);

var closeAgainBlocked = false;
try
{
    await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(primary.MerchantId, MerchantStatus.Closed, actor));
}
catch (InvalidOperationException)
{
    closeAgainBlocked = true;
}
Check("re-closing blocked", closeAgainBlocked, ref passed);

var mutateAfterCloseBlocked = false;
try
{
    await service.SetCapabilitiesAsync(new SetMerchantCapabilitiesCommand(primary.MerchantId, new[] { MerchantCapability.Payouts }, actor));
}
catch (InvalidOperationException)
{
    mutateAfterCloseBlocked = true;
}
Check("mutation blocked after closure", mutateAfterCloseBlocked, ref passed);

// 18-19: missing merchant rejected.
var getMissingBlocked = false;
try
{
    await service.GetAsync("AFM-0000000000000000");
}
catch (KeyNotFoundException)
{
    getMissingBlocked = true;
}
Check("get blocked for missing merchant", getMissingBlocked, ref passed);

var registerMissingBlocked = false;
try
{
    await service.RegisterAsync(new RegisterMerchantCommand("AFM-0000000000000000", actor));
}
catch (KeyNotFoundException)
{
    registerMissingBlocked = true;
}
Check("register blocked for missing merchant", registerMissingBlocked, ref passed);

// 20-24: domain validation guards.
var invalidMerchantIdBlocked = false;
try
{
    _ = new MerchantId("INVALID-000");
}
catch (ArgumentException)
{
    invalidMerchantIdBlocked = true;
}
Check("merchant id without AFM prefix rejected", invalidMerchantIdBlocked, ref passed);

var invalidCountryBlocked = false;
try
{
    MakeProfile("Invalid Country SARL", "Invalid Store", "CIV");
}
catch (ArgumentException)
{
    invalidCountryBlocked = true;
}
Check("invalid country code rejected", invalidCountryBlocked, ref passed);

var invalidCurrencyBlocked = false;
try
{
    _ = new BusinessProfile(
        "Invalid Currency SARL", "Invalid Store", MerchantType.Company, "CI", "XOFF", "Retail", null, null,
        new BusinessAddress("1 Street", null, "Abidjan", "00225", "CI"), new MerchantContact("a@b.com", null));
}
catch (ArgumentException)
{
    invalidCurrencyBlocked = true;
}
Check("invalid settlement currency rejected", invalidCurrencyBlocked, ref passed);

var invalidCityBlocked = false;
try
{
    _ = new BusinessAddress("1 Street", null, " ", "00225", "CI");
}
catch (ArgumentException)
{
    invalidCityBlocked = true;
}
Check("missing city rejected", invalidCityBlocked, ref passed);

var invalidEmailBlocked = false;
try
{
    _ = new MerchantContact(" ", null);
}
catch (ArgumentException)
{
    invalidEmailBlocked = true;
}
Check("missing contact email rejected", invalidEmailBlocked, ref passed);

// 25-26: persistence and audit trail.
var stored = await repository.GetAsync(new MerchantId(fresh.MerchantId));
Check("merchant persisted", stored is not null && stored.MerchantId.ToString() == fresh.MerchantId, ref passed);

var events = await audit.GetAsync(primary.MerchantId);
Check("audit trail exists", events.Count >= 1, ref passed);

// 27-33: financial and enforcement boundary proofs.
Check("KYB not performed", events.All(x => x.Metadata["kybPerformed"] == "false"), ref passed);
Check("payment acceptance not performed", events.All(x => x.Metadata["paymentAcceptancePerformed"] == "false"), ref passed);
Check("payment capture not performed", events.All(x => x.Metadata["paymentCapturePerformed"] == "false"), ref passed);
Check("settlement not performed", events.All(x => x.Metadata["settlementPerformed"] == "false"), ref passed);
Check("payout not performed", events.All(x => x.Metadata["payoutPerformed"] == "false"), ref passed);
Check("money movement not performed", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);
Check("ledger mutation not performed", events.All(x => x.Metadata["ledgerMutationPerformed"] == "false"), ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0019.1 merchant registry scenarios passed.");
Console.WriteLine("Merchant registry: IMPLEMENTED");
Console.WriteLine("Business profile management: IMPLEMENTED");
Console.WriteLine("KYB verification: NOT IMPLEMENTED");
Console.WriteLine("Payment acceptance: NOT IMPLEMENTED");
Console.WriteLine("Payment capture: NOT IMPLEMENTED");
Console.WriteLine("Settlement: NOT IMPLEMENTED");
Console.WriteLine("Payout: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IMerchantClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
