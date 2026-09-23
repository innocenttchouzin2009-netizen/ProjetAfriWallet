namespace AfriWallet.Merchants.TeamAccess.Domain;

public enum MerchantTeamRole { Owner=0, Admin=1, Finance=2, Operator=3, Support=4, ReadOnly=5 }
public enum MerchantTeamPermission {
    ViewMerchant=0, ViewTransactions=1, ViewReceivables=2, ViewPayouts=3,
    ManageTeam=4, ManageSettings=5, InitiateRefund=6, InitiatePayout=7
}

public static class MerchantTeamRolePolicy
{
    public static IReadOnlySet<MerchantTeamPermission> Permissions(MerchantTeamRole role) => role switch
    {
        MerchantTeamRole.Owner => new HashSet<MerchantTeamPermission>(Enum.GetValues<MerchantTeamPermission>()),
        MerchantTeamRole.Admin => new HashSet<MerchantTeamPermission>{
            MerchantTeamPermission.ViewMerchant,MerchantTeamPermission.ViewTransactions,MerchantTeamPermission.ViewReceivables,
            MerchantTeamPermission.ViewPayouts,MerchantTeamPermission.ManageTeam,MerchantTeamPermission.ManageSettings},
        MerchantTeamRole.Finance => new HashSet<MerchantTeamPermission>{
            MerchantTeamPermission.ViewMerchant,MerchantTeamPermission.ViewTransactions,MerchantTeamPermission.ViewReceivables,
            MerchantTeamPermission.ViewPayouts,MerchantTeamPermission.InitiateRefund,MerchantTeamPermission.InitiatePayout},
        MerchantTeamRole.Operator => new HashSet<MerchantTeamPermission>{
            MerchantTeamPermission.ViewMerchant,MerchantTeamPermission.ViewTransactions,MerchantTeamPermission.ViewReceivables},
        MerchantTeamRole.Support => new HashSet<MerchantTeamPermission>{
            MerchantTeamPermission.ViewMerchant,MerchantTeamPermission.ViewTransactions},
        MerchantTeamRole.ReadOnly => new HashSet<MerchantTeamPermission>{
            MerchantTeamPermission.ViewMerchant,MerchantTeamPermission.ViewTransactions,MerchantTeamPermission.ViewReceivables,
            MerchantTeamPermission.ViewPayouts},
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public static bool AllowsMoneyMovement(MerchantTeamRole role) =>
        Permissions(role).Contains(MerchantTeamPermission.InitiatePayout) ||
        Permissions(role).Contains(MerchantTeamPermission.InitiateRefund);
}

public enum MerchantTeamMemberStatus { Active=0, Revoked=1 }

public sealed class MerchantTeamMember
{
    private MerchantTeamMember(Guid id,string merchantId,string awid,MerchantTeamRole role,MerchantTeamMemberStatus status,DateTimeOffset createdAtUtc,DateTimeOffset updatedAtUtc)
    { Id=id; MerchantId=merchantId; Awid=awid; Role=role; Status=status; CreatedAtUtc=createdAtUtc; UpdatedAtUtc=updatedAtUtc; }

    public Guid Id { get; }
    public string MerchantId { get; }
    public string Awid { get; }
    public MerchantTeamRole Role { get; private set; }
    public MerchantTeamMemberStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MerchantTeamMember Create(string merchantId,string awid,MerchantTeamRole role,DateTimeOffset now)
    {
        if(string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant id is required.",nameof(merchantId));
        if(string.IsNullOrWhiteSpace(awid)) throw new ArgumentException("AfWal ID is required.",nameof(awid));
        if(!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
        EnsureUtc(now);
        return new(Guid.NewGuid(),merchantId.Trim().ToUpperInvariant(),awid.Trim(),role,MerchantTeamMemberStatus.Active,now,now);
    }

    public static MerchantTeamMember Restore(Guid id,string merchantId,string awid,MerchantTeamRole role,MerchantTeamMemberStatus status,DateTimeOffset createdAtUtc,DateTimeOffset updatedAtUtc)
    {
        if(id==Guid.Empty) throw new ArgumentException("Member id is required.",nameof(id));
        if(!Enum.IsDefined(role)||!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException();
        EnsureUtc(createdAtUtc); EnsureUtc(updatedAtUtc);
        return new(id,merchantId,awid,role,status,createdAtUtc,updatedAtUtc);
    }

    public void ChangeRole(MerchantTeamRole role,DateTimeOffset now)
    {
        if(Status!=MerchantTeamMemberStatus.Active) throw new InvalidOperationException("Revoked member is immutable.");
        if(Role==MerchantTeamRole.Owner) throw new InvalidOperationException("Owner role cannot be changed through team access.");
        if(role==MerchantTeamRole.Owner) throw new InvalidOperationException("Owner role can only come from Merchant Registry.");
        EnsureUtc(now); Role=role; UpdatedAtUtc=now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if(Role==MerchantTeamRole.Owner) throw new InvalidOperationException("Merchant owner cannot be revoked.");
        EnsureUtc(now); Status=MerchantTeamMemberStatus.Revoked; UpdatedAtUtc=now;
    }

    private static void EnsureUtc(DateTimeOffset value){ if(value.Offset!=TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC."); }
}
