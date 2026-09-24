using AfriWallet.Merchants.Intelligence.Application.Abstractions;
using AfriWallet.Merchants.Intelligence.Domain.Findings;
using AfriWallet.Merchants.Intelligence.Domain.Metrics;
using AfriWallet.Merchants.Intelligence.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Assert(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
var path=Path.Combine(Path.GetTempPath(),$"afwal-merchant-intelligence-{Guid.NewGuid():N}.db");
var options=new DbContextOptionsBuilder<MerchantIntelligenceDbContext>().UseSqlite($"Data Source={path}").Options;
var findingId=Guid.NewGuid(); var eventId=Guid.NewGuid();
var metrics=new MerchantCommerceMetrics(10,8,7,3,2,6,5,1,1,1,1,0,.3,.2,.2);
var patterns=new[]{new MerchantRiskPattern("MER-INT-DURABLE",20,"Durability scenario",["payment-1"])};
try{
 await using(var db=new MerchantIntelligenceDbContext(options)){
  await db.Database.EnsureCreatedAsync();
  var repo=new EfMerchantIntelligenceRepository(db); var audit=new EfMerchantIntelligenceAuditStore(db);
  await repo.SaveAsync(new MerchantRiskFinding(findingId,"AFM-DURABLE",20,MerchantRiskSeverity.Medium,MerchantProtectionRecommendation.Monitor,metrics,patterns,DateTimeOffset.UtcNow));
  await audit.AppendAsync(new MerchantIntelligenceAuditEvent(eventId,findingId,"AFM-DURABLE","MerchantRiskEvaluated","scenario",DateTimeOffset.UtcNow,new Dictionary<string,string>{{"moneyMovementPerformed","false"}}));
 }
 await using(var db=new MerchantIntelligenceDbContext(options)){
  var repo=new EfMerchantIntelligenceRepository(db); var audit=new EfMerchantIntelligenceAuditStore(db);
  var restored=await repo.GetLatestAsync("AFM-DURABLE");
  Assert(restored is not null && restored.FindingId==findingId,"Finding did not survive restart.");
  Assert(restored!.Metrics.CheckoutCount==10 && restored.Patterns.Single().Code=="MER-INT-DURABLE","Finding payload did not survive restart.");
  var events=await audit.GetAsync(findingId);
  Assert(events.Count==1 && events.Single().EventId==eventId,"Audit did not survive restart.");
  Assert(events.Single().Metadata["moneyMovementPerformed"]=="false","Audit metadata boundary was not preserved.");
 }
 Console.WriteLine("AFW-BE-MERCHANT-INTELLIGENCE-DURABILITY-1 restart-safe scenarios: PASS");
} finally { if(File.Exists(path))File.Delete(path); }
