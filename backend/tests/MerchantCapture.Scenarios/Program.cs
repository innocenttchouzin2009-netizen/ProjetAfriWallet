using AfriWallet.Merchants.Capture.Application;
using AfriWallet.Merchants.Capture.Domain;
using AfriWallet.Merchants.Capture.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Check(string name,bool ok,ref int passed){Console.WriteLine($"{name,-58} {(ok?"PASS":"FAIL")}");if(!ok)throw new InvalidOperationException(name);passed++;}

var passed=0;
var dbPath=Path.Combine(Path.GetTempPath(),$"afwal-merchant-capture-{Guid.NewGuid():N}.db");
var options=new DbContextOptionsBuilder<MerchantCaptureDbContext>().UseSqlite($"Data Source={dbPath}").Options;
var clock=new FixedTimeProvider(new DateTimeOffset(2026,9,22,18,0,0,TimeSpan.Zero));
var decisionId=Guid.NewGuid();
var intentId=Guid.NewGuid();
var reader=new ScenarioDecisionReader();
reader.Set(new(decisionId,intentId,"AFM-100","CaptureEligible","Approved",12500,"XAF","Active","Verified"));
var provider=new IdempotentSandboxMerchantCaptureProvider();

try
{
    Guid executionId;
    await using(var db=new MerchantCaptureDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();
        var service=new MerchantCaptureService(new EfMerchantCaptureRepository(db),reader,provider,new EfMerchantCaptureAuditStore(db),clock);
        var first=await service.ExecuteAsync(new(decisionId,"capture-key-1","scenario"));
        executionId=first.ExecutionId;
        Check("capture completed",first.Status==MerchantCaptureStatus.Captured,ref passed);
        Check("provider reference persisted",!string.IsNullOrWhiteSpace(first.ProviderReference),ref passed);
        Check("capture ready for settlement",first.SettlementReady,ref passed);
        Check("provider called once",provider.Calls==1,ref passed);
        var events=await new EfMerchantCaptureAuditStore(db).ListAsync(first.ExecutionId);
        Check("audit created/completed",events.Count==2,ref passed);
        var createdAudit=events.Single(x=>x.EventType=="capture.created");
        var completedAudit=events.Single(x=>x.EventType=="capture.completed");
        Check("audit capture false at creation",createdAudit.Metadata["captureExecutionPerformed"]=="false",ref passed);
        Check("audit capture true after completion",completedAudit.Metadata["captureExecutionPerformed"]=="true",ref passed);
        Check("audit settlement remains false",events.All(x=>x.Metadata["settlementPerformed"]=="false"),ref passed);
        Check("audit ledger mutation remains false",events.All(x=>x.Metadata["ledgerMutationPerformed"]=="false"),ref passed);
    }

    await using(var db=new MerchantCaptureDbContext(options))
    {
        var service=new MerchantCaptureService(new EfMerchantCaptureRepository(db),reader,provider,new EfMerchantCaptureAuditStore(db),clock);
        var again=await service.ExecuteAsync(new(decisionId,"capture-key-1","scenario-restart"));
        Check("restart idempotency preserves execution",again.ExecutionId==executionId,ref passed);
        Check("restart does not call provider again",provider.Calls==1,ref passed);
    }

    var invalidDecision=Guid.NewGuid();
    reader.Set(new(invalidDecision,Guid.NewGuid(),"AFM-100","Authorize","Approved",12500,"XAF","Active","Verified"));
    await using(var db=new MerchantCaptureDbContext(options))
    {
        var service=new MerchantCaptureService(new EfMerchantCaptureRepository(db),reader,provider,new EfMerchantCaptureAuditStore(db),clock);
        var blocked=false;try{await service.ExecuteAsync(new(invalidDecision,"capture-key-invalid","scenario"));}catch(InvalidOperationException){blocked=true;}
        Check("non CaptureEligible decision blocked",blocked,ref passed);
    }

    var badMerchant=Guid.NewGuid();
    reader.Set(new(badMerchant,Guid.NewGuid(),"AFM-200","CaptureEligible","Approved",5000,"EUR","Suspended","Verified"));
    await using(var db=new MerchantCaptureDbContext(options))
    {
        var service=new MerchantCaptureService(new EfMerchantCaptureRepository(db),reader,provider,new EfMerchantCaptureAuditStore(db),clock);
        var blocked=false;try{await service.ExecuteAsync(new(badMerchant,"capture-key-merchant","scenario"));}catch(InvalidOperationException){blocked=true;}
        Check("ineligible merchant blocked",blocked,ref passed);
    }

    var failureDecision=Guid.NewGuid();
    reader.Set(new(failureDecision,Guid.NewGuid(),"AFM-300","CaptureEligible","Approved",7000,"USD","Active","Verified"));
    await using(var db=new MerchantCaptureDbContext(options))
    {
        var failing=new FailingProvider();
        var service=new MerchantCaptureService(new EfMerchantCaptureRepository(db),reader,failing,new EfMerchantCaptureAuditStore(db),clock);
        var failed=await service.ExecuteAsync(new(failureDecision,"capture-key-failure","scenario"));
        Check("provider failure persisted terminally",failed.Status==MerchantCaptureStatus.Failed&&failed.FailureCode=="provider_declined",ref passed);
        var again=await service.ExecuteAsync(new(failureDecision,"capture-key-failure","scenario"));
        Check("failed execution is idempotent",again.ExecutionId==failed.ExecutionId&&failing.Calls==1,ref passed);
    }

    Console.WriteLine();
    Console.WriteLine($"Checks: {passed}");
    Console.WriteLine($"Passed: {passed}");
    Console.WriteLine("Failed: 0");
    Console.WriteLine("Skipped: 0");
    Console.WriteLine("AFW-BE-MERCHANT-CAPTURE-1 durable merchant capture execution scenarios: PASS");
}
finally
{
    if(File.Exists(dbPath))File.Delete(dbPath);
}

sealed class ScenarioDecisionReader:ICaptureEligibleDecisionReader
{
    private readonly Dictionary<Guid,CaptureEligibleDecisionSnapshot> items=new();
    public void Set(CaptureEligibleDecisionSnapshot value)=>items[value.DecisionId]=value;
    public Task<CaptureEligibleDecisionSnapshot?> GetAsync(Guid id,CancellationToken ct=default){ct.ThrowIfCancellationRequested();items.TryGetValue(id,out var value);return Task.FromResult(value);}
}
sealed class FailingProvider:IMerchantCaptureProvider
{
    public int Calls{get;private set;}
    public Task<MerchantCaptureProviderResult> CaptureAsync(MerchantCaptureProviderRequest request,CancellationToken ct=default){Calls++;return Task.FromResult(new MerchantCaptureProviderResult(false,null,"provider_declined"));}
}
sealed class FixedTimeProvider(DateTimeOffset now):TimeProvider{public override DateTimeOffset GetUtcNow()=>now;}
