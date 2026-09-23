using AfriWallet.Merchants.TeamAccess.Application;
using AfriWallet.Merchants.TeamAccess.Domain;
using AfriWallet.Merchants.TeamAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Check(string n,bool ok,ref int p){Console.WriteLine($"{n,-64} {(ok?"PASS":"FAIL")}");if(!ok)throw new InvalidOperationException(n);p++;}
var passed=0; var path=Path.Combine(Path.GetTempPath(),$"afwal-mta-{Guid.NewGuid():N}.db");
var opts=new DbContextOptionsBuilder<MerchantTeamAccessDbContext>().UseSqlite($"Data Source={path}").Options;
var clock=new FixedTimeProvider(new DateTimeOffset(2026,9,23,20,0,0,TimeSpan.Zero));
var owners=new OwnerReader(new("AFM-TEAM-001","owner.awid"));
try{
 await using(var db=new MerchantTeamAccessDbContext(opts)){
  await db.Database.EnsureCreatedAsync();
  var repo=new EfMerchantTeamRepository(db);var audit=new EfMerchantTeamAuditStore(db);var svc=new MerchantTeamAccessService(owners,repo,audit,clock);
  var owner=await svc.EnsureOwnerAsync("AFM-TEAM-001","scenario");
  Check("owner bootstraps from merchant registry",owner.Role==MerchantTeamRole.Owner,ref passed);
  var ro=await svc.AddMemberAsync("AFM-TEAM-001","reader.awid",MerchantTeamRole.ReadOnly,"scenario");
  Check("read-only member added",ro.Status==MerchantTeamMemberStatus.Active,ref passed);
  Check("read-only can view payouts",await svc.IsAllowedAsync("AFM-TEAM-001","reader.awid",MerchantTeamPermission.ViewPayouts),ref passed);
  Check("read-only cannot initiate payout",!await svc.IsAllowedAsync("AFM-TEAM-001","reader.awid",MerchantTeamPermission.InitiatePayout),ref passed);
  Check("read-only role has no money movement",!MerchantTeamRolePolicy.AllowsMoneyMovement(MerchantTeamRole.ReadOnly),ref passed);
  var finance=await svc.AddMemberAsync("AFM-TEAM-001","finance.awid",MerchantTeamRole.Finance,"scenario");
  Check("finance may initiate payout",await svc.IsAllowedAsync("AFM-TEAM-001",finance.Awid,MerchantTeamPermission.InitiatePayout),ref passed);
  await svc.ChangeRoleAsync("AFM-TEAM-001","finance.awid",MerchantTeamRole.Support,"scenario");
  Check("role change revokes payout capability",!await svc.IsAllowedAsync("AFM-TEAM-001","finance.awid",MerchantTeamPermission.InitiatePayout),ref passed);
  await svc.RevokeAsync("AFM-TEAM-001","reader.awid","scenario");
  Check("revoked member loses read access",!await svc.IsAllowedAsync("AFM-TEAM-001","reader.awid",MerchantTeamPermission.ViewMerchant),ref passed);
  var ownerProtected=false;try{await svc.RevokeAsync("AFM-TEAM-001","owner.awid","scenario");}catch(InvalidOperationException){ownerProtected=true;}
  Check("merchant owner cannot be revoked",ownerProtected,ref passed);
  var events=await audit.ListAsync("AFM-TEAM-001");
  Check("team changes are audited",events.Count>=5,ref passed);
 }
 await using(var restarted=new MerchantTeamAccessDbContext(opts)){
  var repo=new EfMerchantTeamRepository(restarted);var list=await repo.ListAsync("AFM-TEAM-001");
  Check("team membership survives restart",list.Count==3,ref passed);
 }
 Console.WriteLine($"
AFW-BE-MERCHANT-TEAM-ACCESS-1 scenarios: PASS ({passed})");
}finally{if(File.Exists(path))File.Delete(path);}
sealed class OwnerReader(MerchantOwnerSnapshot value):IMerchantOwnerReader{public Task<MerchantOwnerSnapshot?> GetAsync(string id,CancellationToken ct=default){ct.ThrowIfCancellationRequested();return Task.FromResult<MerchantOwnerSnapshot?>(string.Equals(id,value.MerchantId,StringComparison.OrdinalIgnoreCase)?value:null);}}
sealed class FixedTimeProvider(DateTimeOffset now):TimeProvider{public override DateTimeOffset GetUtcNow()=>now;}
