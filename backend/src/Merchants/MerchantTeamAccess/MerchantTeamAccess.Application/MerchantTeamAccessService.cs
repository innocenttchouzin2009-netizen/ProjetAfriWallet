using AfriWallet.Merchants.TeamAccess.Domain;

namespace AfriWallet.Merchants.TeamAccess.Application;

public sealed record MerchantOwnerSnapshot(string MerchantId,string OwnerAwid);
public interface IMerchantOwnerReader { Task<MerchantOwnerSnapshot?> GetAsync(string merchantId,CancellationToken cancellationToken=default); }
public interface IMerchantTeamRepository
{
    Task AddAsync(MerchantTeamMember member,CancellationToken cancellationToken=default);
    Task SaveAsync(MerchantTeamMember member,CancellationToken cancellationToken=default);
    Task<MerchantTeamMember?> GetByAwidAsync(string merchantId,string awid,CancellationToken cancellationToken=default);
    Task<IReadOnlyCollection<MerchantTeamMember>> ListAsync(string merchantId,CancellationToken cancellationToken=default);
}
public sealed record MerchantTeamAuditEvent(Guid EventId,Guid MemberId,string MerchantId,string Awid,string EventType,string Actor,DateTimeOffset OccurredAtUtc,IReadOnlyDictionary<string,string> Metadata);
public interface IMerchantTeamAuditStore
{
    Task AppendAsync(MerchantTeamAuditEvent auditEvent,CancellationToken cancellationToken=default);
    Task<IReadOnlyCollection<MerchantTeamAuditEvent>> ListAsync(string merchantId,CancellationToken cancellationToken=default);
}

public sealed class MerchantTeamAccessService(IMerchantOwnerReader owners,IMerchantTeamRepository members,IMerchantTeamAuditStore audit,TimeProvider timeProvider)
{
    public async Task<MerchantTeamMember> EnsureOwnerAsync(string merchantId,string actor,CancellationToken ct=default)
    {
        var owner=await owners.GetAsync(merchantId,ct)??throw new KeyNotFoundException("Merchant not found.");
        var existing=await members.GetByAwidAsync(owner.MerchantId,owner.OwnerAwid,ct);
        if(existing is not null) return existing;
        var member=MerchantTeamMember.Create(owner.MerchantId,owner.OwnerAwid,MerchantTeamRole.Owner,timeProvider.GetUtcNow());
        await members.AddAsync(member,ct); await Write(member,"merchant.team.owner_bootstrapped",actor,ct); return member;
    }

    public async Task<MerchantTeamMember> AddMemberAsync(string merchantId,string awid,MerchantTeamRole role,string actor,CancellationToken ct=default)
    {
        if(role==MerchantTeamRole.Owner) throw new InvalidOperationException("Owner is controlled by Merchant Registry.");
        await EnsureOwnerAsync(merchantId,actor,ct);
        var existing=await members.GetByAwidAsync(merchantId,awid,ct);
        if(existing is not null) throw new InvalidOperationException("AfWal ID is already linked to this merchant.");
        var member=MerchantTeamMember.Create(merchantId,awid,role,timeProvider.GetUtcNow());
        await members.AddAsync(member,ct); await Write(member,"merchant.team.member_added",actor,ct); return member;
    }

    public async Task<MerchantTeamMember> ChangeRoleAsync(string merchantId,string awid,MerchantTeamRole role,string actor,CancellationToken ct=default)
    {
        var member=await members.GetByAwidAsync(merchantId,awid,ct)??throw new KeyNotFoundException("Merchant team member not found.");
        member.ChangeRole(role,timeProvider.GetUtcNow()); await members.SaveAsync(member,ct); await Write(member,"merchant.team.role_changed",actor,ct); return member;
    }

    public async Task<MerchantTeamMember> RevokeAsync(string merchantId,string awid,string actor,CancellationToken ct=default)
    {
        var member=await members.GetByAwidAsync(merchantId,awid,ct)??throw new KeyNotFoundException("Merchant team member not found.");
        member.Revoke(timeProvider.GetUtcNow()); await members.SaveAsync(member,ct); await Write(member,"merchant.team.member_revoked",actor,ct); return member;
    }

    public Task<IReadOnlyCollection<MerchantTeamMember>> ListAsync(string merchantId,CancellationToken ct=default)=>members.ListAsync(merchantId,ct);

    public async Task<bool> IsAllowedAsync(string merchantId,string awid,MerchantTeamPermission permission,CancellationToken ct=default)
    {
        var member=await members.GetByAwidAsync(merchantId,awid,ct);
        return member is not null && member.Status==MerchantTeamMemberStatus.Active && MerchantTeamRolePolicy.Permissions(member.Role).Contains(permission);
    }

    private Task Write(MerchantTeamMember member,string type,string actor,CancellationToken ct)=>audit.AppendAsync(
        new(Guid.NewGuid(),member.Id,member.MerchantId,member.Awid,type,string.IsNullOrWhiteSpace(actor)?"system":actor.Trim(),timeProvider.GetUtcNow(),
        new Dictionary<string,string>{
            ["role"]=member.Role.ToString(),["status"]=member.Status.ToString(),
            ["moneyMovementAllowed"]=MerchantTeamRolePolicy.AllowsMoneyMovement(member.Role).ToString().ToLowerInvariant()
        }),ct);
}
