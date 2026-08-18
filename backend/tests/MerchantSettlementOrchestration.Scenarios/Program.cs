using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Application.Commands;
using AfriWallet.Merchants.Settlement.Application.Policies;
using AfriWallet.Merchants.Settlement.Application.Services;
using AfriWallet.Merchants.Settlement.Domain.Settlements;
using AfriWallet.Merchants.Settlement.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-54} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2027, 5, 6, 9, 0, 0, TimeSpan.Zero);
const string actor = "scenario-runner";
var reader = new SandboxCaptureEligibleDecisionReader();
var repository = new InMemoryMerchantSettlementRepository();
var provider = new SandboxMerchantSettlementProvider();
var audit = new InMemoryMerchantSettlementAuditStore();
var service = new MerchantSettlementService(repository, reader, provider, audit, new FixedClock(now), new MerchantSettlementRoutingPolicy(), new MerchantSettlementRetryPolicy());

Guid Seed(string type = "CaptureEligible", string status = "Approved", string registry = "Active", string verification = "Verified", long amount = 1000)
{
    var id = Guid.NewGuid();
    reader.Set(new CaptureEligibleDecisionSnapshot(id, Guid.NewGuid(), "AFM-SETTLE", type, status, amount, "XOF", registry, verification));
    return id;
}

var missing = false;
try { await service.CreateAsync(new CreateMerchantSettlementCommand(Guid.NewGuid(), MerchantSettlementRoute.MerchantSettlement, "missing", actor)); } catch (KeyNotFoundException) { missing = true; }
Check("missing decision blocked", missing, ref passed);
var ineligible = false;
try { await service.CreateAsync(new CreateMerchantSettlementCommand(Seed(type: "Authorize"), MerchantSettlementRoute.MerchantSettlement, "bad", actor)); } catch (InvalidOperationException) { ineligible = true; }
Check("non capture eligible decision blocked", ineligible, ref passed);
var inactive = false;
try { await service.CreateAsync(new CreateMerchantSettlementCommand(Seed(registry: "Suspended"), MerchantSettlementRoute.MerchantSettlement, "bad2", actor)); } catch (InvalidOperationException) { inactive = true; }
Check("inactive merchant blocked", inactive, ref passed);

var decision = Seed();
var settlement = await service.CreateAsync(new CreateMerchantSettlementCommand(decision, MerchantSettlementRoute.MerchantSettlement, "idem-1", actor));
Check("settlement created", settlement.Status == MerchantSettlementStatus.Created, ref passed);
Check("settlement route selected", settlement.Route == MerchantSettlementRoute.MerchantSettlement, ref passed);
var repeat = await service.CreateAsync(new CreateMerchantSettlementCommand(decision, MerchantSettlementRoute.MerchantSettlement, "idem-1", actor));
Check("idempotency returns same settlement", repeat.SettlementId == settlement.SettlementId, ref passed);
var decisionRepeat = await service.CreateAsync(new CreateMerchantSettlementCommand(decision, MerchantSettlementRoute.MerchantPayout, "idem-2", actor));
Check("decision prevents duplicate orchestration", decisionRepeat.SettlementId == settlement.SettlementId, ref passed);

provider.Enqueue(MerchantSettlementProviderStatus.Accepted);
var acknowledged = await service.DispatchAsync(new DispatchMerchantSettlementCommand(settlement.SettlementId, actor));
Check("accepted dispatch acknowledged", acknowledged.Status == MerchantSettlementStatus.Acknowledged, ref passed);
Check("accepted dispatch tracks attempt", acknowledged.AttemptCount == 1, ref passed);
Check("provider reference tracked", !string.IsNullOrWhiteSpace(acknowledged.ProviderReference), ref passed);
var completed = await service.CompleteAsync(new CompleteMerchantSettlementCommand(settlement.SettlementId, actor));
Check("acknowledged settlement completes", completed.Status == MerchantSettlementStatus.Completed, ref passed);

var partial = await service.CreateAsync(new CreateMerchantSettlementCommand(Seed(), MerchantSettlementRoute.MerchantPayout, "partial", actor));
provider.Enqueue(MerchantSettlementProviderStatus.PartialFailure);
var compensation = await service.DispatchAsync(new DispatchMerchantSettlementCommand(partial.SettlementId, actor));
Check("partial failure requires compensation", compensation.Status == MerchantSettlementStatus.CompensationRequired, ref passed);
Check("compensation record created", compensation.CompensationCount == 1, ref passed);
var compensated = await service.CompensateAsync(new CompensateMerchantSettlementCommand(partial.SettlementId, actor));
Check("compensation completes", compensated.Status == MerchantSettlementStatus.Compensated, ref passed);
var completedComp = await service.CompleteAsync(new CompleteMerchantSettlementCommand(partial.SettlementId, actor));
Check("compensated settlement completes", completedComp.Status == MerchantSettlementStatus.Completed, ref passed);

var retry = await service.CreateAsync(new CreateMerchantSettlementCommand(Seed(), MerchantSettlementRoute.MerchantSettlement, "retry", actor));
provider.Enqueue(MerchantSettlementProviderStatus.TemporaryFailure);
var first = await service.DispatchAsync(new DispatchMerchantSettlementCommand(retry.SettlementId, actor));
Check("temporary failure schedules retry", first.Status == MerchantSettlementStatus.RetryPending, ref passed);
provider.Enqueue(MerchantSettlementProviderStatus.Timeout);
var second = await service.RetryAsync(new RetryMerchantSettlementCommand(retry.SettlementId, actor));
Check("timeout schedules second retry", second.Status == MerchantSettlementStatus.RetryPending, ref passed);
provider.Enqueue(MerchantSettlementProviderStatus.TemporaryFailure);
var exhausted = await service.RetryAsync(new RetryMerchantSettlementCommand(retry.SettlementId, actor));
Check("retry exhaustion needs intervention", exhausted.Status == MerchantSettlementStatus.ManualInterventionRequired, ref passed);
Check("retry exhaustion reason", exhausted.ReasonCode == MerchantSettlementReasonCode.RetryExhausted, ref passed);

var permanent = await service.CreateAsync(new CreateMerchantSettlementCommand(Seed(), MerchantSettlementRoute.MerchantSettlement, "permanent", actor));
provider.Enqueue(MerchantSettlementProviderStatus.PermanentFailure);
var failed = await service.DispatchAsync(new DispatchMerchantSettlementCommand(permanent.SettlementId, actor));
Check("permanent failure fails", failed.Status == MerchantSettlementStatus.Failed, ref passed);
var dispatchFailed = false;
try { await service.DispatchAsync(new DispatchMerchantSettlementCommand(permanent.SettlementId, actor)); } catch (InvalidOperationException) { dispatchFailed = true; }
Check("terminal failed settlement immutable", dispatchFailed, ref passed);

var missingGet = false;
try { await service.GetAsync(Guid.NewGuid()); } catch (KeyNotFoundException) { missingGet = true; }
Check("missing settlement blocked", missingGet, ref passed);
var events = await audit.GetAsync(settlement.SettlementId);
Check("audit exists", events.Count >= 3, ref passed);
Check("real capture absent", events.All(x => x.Metadata["realCapturePerformed"] == "false"), ref passed);
Check("real settlement absent", events.All(x => x.Metadata["realSettlementPerformed"] == "false"), ref passed);
Check("real payout absent", events.All(x => x.Metadata["realPayoutPerformed"] == "false"), ref passed);
Check("merchant funds absent", events.All(x => x.Metadata["merchantFundsMoved"] == "false"), ref passed);
Check("wallet mutation absent", events.All(x => x.Metadata["walletBalanceMutated"] == "false"), ref passed);
Check("ledger mutation absent", events.All(x => x.Metadata["directLedgerMutationPerformed"] == "false"), ref passed);
Check("external settlement absent", events.All(x => x.Metadata["externalSettlementPerformed"] == "false"), ref passed);

Console.WriteLine($"\nChecks: {passed}\nPassed: {passed}\nFailed: 0\nSkipped: 0\n");
Console.WriteLine("All AFW-DLV-0019.5 merchant settlement scenarios passed.");
Console.WriteLine("Settlement orchestration: IMPLEMENTED\nPayout orchestration: IMPLEMENTED\nIdempotency: IMPLEMENTED\nRetry policy: IMPLEMENTED\nCompensation workflow: IMPLEMENTED\nReal capture: NOT IMPLEMENTED\nReal settlement: NOT IMPLEMENTED\nReal payout: NOT IMPLEMENTED\nMoney movement: NOT IMPLEMENTED\nWallet mutation: NOT IMPLEMENTED\nLedger mutation: NOT IMPLEMENTED\nDecision: READY FOR REVIEW");
sealed class FixedClock(DateTimeOffset n) : IMerchantSettlementClock { public DateTimeOffset UtcNow { get; } = n; }
