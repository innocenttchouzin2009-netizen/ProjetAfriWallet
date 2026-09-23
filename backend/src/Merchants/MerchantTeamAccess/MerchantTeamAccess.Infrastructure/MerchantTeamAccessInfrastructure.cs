using System.Text.Json;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.TeamAccess.Application;
using AfriWallet.Merchants.TeamAccess.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.TeamAccess.Infrastructure;

public sealed class MerchantTeamAccessDbContext(DbContextOptions<MerchantTeamAccessDbContext> options):DbContext(options)
{
    public DbSet<MerchantTeamMemberEntity> Members=>Set<MerchantTeamMemberEntity>();
    public DbSet<MerchantTeamAuditEntity> Audit=>Set<MerchantTeamAuditEntity>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<MerchantTeamMemberEntity>(e=>{e.ToTable("MerchantTeamMembers");e.HasKey(x=>x.Id);e.Property(x=>x.MerchantId).HasMaxLength(128).IsRequired();e.Property(x=>x.Awid).HasMaxLength(128).IsRequired();e.HasIndex(x=>new{x.MerchantId,x.Awid}).IsUnique();});
        b.Entity<MerchantTeamAuditEntity>(e=>{e.ToTable("MerchantTeamAudit");e.HasKey(x=>x.EventId);e.HasIndex(x=>new{x.MerchantId,x.OccurredAtUtc});});
    }
}
public sealed class MerchantTeamMemberEntity{public Guid Id{get;set;} public string MerchantId{get;set;}=""; public string Awid{get;set;}=""; public int Role{get;set;} public int Status{get;set;} public DateTimeOffset CreatedAtUtc{get;set;} public DateTimeOffset UpdatedAtUtc{get;set;}}
public sealed class MerchantTeamAuditEntity{public Guid EventId{get;set;} public Guid MemberId{get;set;} public string MerchantId{get;set;}=""; public string Awid{get;set;}=""; public string EventType{get;set;}=""; public string Actor{get;set;}=""; public DateTimeOffset OccurredAtUtc{get;set;} public string MetadataJson{get;set;}="{}";}

public sealed class EfMerchantTeamRepository(MerchantTeamAccessDbContext db):IMerchantTeamRepository
{
    public async Task AddAsync(MerchantTeamMember m,CancellationToken ct=default){db.Members.Add(Map(m));await db.SaveChangesAsync(ct);}
    public async Task SaveAsync(MerchantTeamMember m,CancellationToken ct=default){var r=await db.Members.SingleAsync(x=>x.Id==m.Id,ct);Copy(m,r);await db.SaveChangesAsync(ct);}
    public async Task<MerchantTeamMember?> GetByAwidAsync(string merchantId,string awid,CancellationToken ct=default){var r=await db.Members.AsNoTracking().SingleOrDefaultAsync(x=>x.MerchantId==merchantId.Trim().ToUpper()&&x.Awid==awid.Trim(),ct);return r is null?null:Map(r);}
    public async Task<IReadOnlyCollection<MerchantTeamMember>> ListAsync(string merchantId,CancellationToken ct=default)=>(await db.Members.AsNoTracking().Where(x=>x.MerchantId==merchantId.Trim().ToUpper()).ToListAsync(ct)).OrderBy(x=>x.Awid).Select(Map).ToArray();
    static MerchantTeamMemberEntity Map(MerchantTeamMember m){var r=new MerchantTeamMemberEntity{Id=m.Id};Copy(m,r);return r;}
    static void Copy(MerchantTeamMember m,MerchantTeamMemberEntity r){r.MerchantId=m.MerchantId;r.Awid=m.Awid;r.Role=(int)m.Role;r.Status=(int)m.Status;r.CreatedAtUtc=m.CreatedAtUtc;r.UpdatedAtUtc=m.UpdatedAtUtc;}
    static MerchantTeamMember Map(MerchantTeamMemberEntity r)=>MerchantTeamMember.Restore(r.Id,r.MerchantId,r.Awid,(MerchantTeamRole)r.Role,(MerchantTeamMemberStatus)r.Status,r.CreatedAtUtc,r.UpdatedAtUtc);
}
public sealed class EfMerchantTeamAuditStore(MerchantTeamAccessDbContext db):IMerchantTeamAuditStore
{
    public async Task AppendAsync(MerchantTeamAuditEvent e,CancellationToken ct=default){db.Audit.Add(new(){EventId=e.EventId,MemberId=e.MemberId,MerchantId=e.MerchantId,Awid=e.Awid,EventType=e.EventType,Actor=e.Actor,OccurredAtUtc=e.OccurredAtUtc,MetadataJson=JsonSerializer.Serialize(e.Metadata)});await db.SaveChangesAsync(ct);}
    public async Task<IReadOnlyCollection<MerchantTeamAuditEvent>> ListAsync(string merchantId,CancellationToken ct=default){var rows=await db.Audit.AsNoTracking().Where(x=>x.MerchantId==merchantId.Trim().ToUpper()).ToListAsync(ct);return rows.OrderBy(x=>x.OccurredAtUtc).ThenBy(x=>x.EventId).Select(x=>new MerchantTeamAuditEvent(x.EventId,x.MemberId,x.MerchantId,x.Awid,x.EventType,x.Actor,x.OccurredAtUtc,JsonSerializer.Deserialize<Dictionary<string,string>>(x.MetadataJson)??new())).ToArray();}
}
public sealed class MerchantRegistryOwnerReader(IMerchantRepository merchants):IMerchantOwnerReader
{
    public async Task<MerchantOwnerSnapshot?> GetAsync(string merchantId,CancellationToken ct=default)
    {
        Merchant m; try{m=await merchants.GetAsync(new MerchantId(merchantId),ct)??throw new KeyNotFoundException();}catch{return null;}
        return new(m.MerchantId.Value,m.OwnerAwid);
    }
}
