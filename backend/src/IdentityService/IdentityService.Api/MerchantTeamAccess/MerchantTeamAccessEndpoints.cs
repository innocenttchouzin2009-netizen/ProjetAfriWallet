using System.Security.Claims;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.TeamAccess.Application;
using AfriWallet.Merchants.TeamAccess.Domain;
using AfriWallet.P2P.Directory.Persistence;
using AfriWallet.P2P.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.MerchantTeamAccess;

public static class MerchantTeamAccessEndpoints
{
    public static IEndpointRouteBuilder MapMerchantTeamAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/merchants/{merchantId}/team")
            .RequireAuthorization();

        group.MapGet("", ListAsync);
        group.MapGet("/me", GetMeAsync);
        group.MapPost("", AddAsync);
        group.MapPut("/{awid}/role", ChangeRoleAsync);
        group.MapPost("/{awid}/revoke", RevokeAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        string merchantId,
        ClaimsPrincipal principal,
        MerchantTeamAccessService service,
        IMerchantRepository merchants,
        RecipientDirectoryDbContext identities,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(
            merchantId, MerchantTeamPermission.ViewMerchant, principal, service, merchants, identities, cancellationToken);
        if (actor.Status != ActorStatus.Allowed)
            return ActorError(actor.Status, httpContext);

        var members = await service.ListAsync(merchantId, cancellationToken);
        return Results.Ok(members.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> GetMeAsync(
        string merchantId,
        ClaimsPrincipal principal,
        MerchantTeamAccessService service,
        IMerchantRepository merchants,
        RecipientDirectoryDbContext identities,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(
            merchantId, MerchantTeamPermission.ViewMerchant, principal, service, merchants, identities, cancellationToken);
        if (actor.Status != ActorStatus.Allowed || string.IsNullOrWhiteSpace(actor.Awid))
            return ActorError(actor.Status, httpContext);

        var member = (await service.ListAsync(merchantId, cancellationToken))
            .FirstOrDefault(x =>
                x.Status == MerchantTeamMemberStatus.Active &&
                string.Equals(x.Awid, actor.Awid, StringComparison.OrdinalIgnoreCase));

        return member is null ? NotFound(httpContext) : Results.Ok(ToResponse(member));
    }

    private static async Task<IResult> AddAsync(
        string merchantId,
        AddMerchantTeamMemberRequest request,
        ClaimsPrincipal principal,
        MerchantTeamAccessService service,
        IMerchantRepository merchants,
        RecipientDirectoryDbContext identities,
        IAfWalIdentityDirectory identityDirectory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(
            merchantId, MerchantTeamPermission.ManageTeam, principal, service, merchants, identities, cancellationToken);
        if (actor.Status != ActorStatus.Allowed || string.IsNullOrWhiteSpace(actor.Awid))
            return ActorError(actor.Status, httpContext);

        if (!TryRole(request.Role, out var role))
            return Validation(httpContext, "Merchant team role is invalid.");
        if (role == MerchantTeamRole.Owner)
            return Validation(httpContext, "Owner role is controlled by Merchant Registry.");

        string normalizedAwid;
        try
        {
            normalizedAwid = RecipientDirectoryNormalization.NormalizeAfWalId(request.AfWalId);
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }

        if (await identityDirectory.ResolveOwnerIdAsync(normalizedAwid, cancellationToken) is null)
            return Results.NotFound(new MerchantTeamErrorResponse(
                MerchantTeamErrorCode.NotFound,
                "AfWal ID was not found or is inactive.",
                httpContext.TraceIdentifier));

        try
        {
            var member = await service.AddMemberAsync(
                merchantId, normalizedAwid, role, actor.Awid, cancellationToken);
            return Results.Created(
                $"/api/v1/merchants/{merchantId}/team/{Uri.EscapeDataString(member.Awid)}",
                ToResponse(member));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(httpContext);
        }
    }

    private static async Task<IResult> ChangeRoleAsync(
        string merchantId,
        string awid,
        ChangeMerchantTeamMemberRoleRequest request,
        ClaimsPrincipal principal,
        MerchantTeamAccessService service,
        IMerchantRepository merchants,
        RecipientDirectoryDbContext identities,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(
            merchantId, MerchantTeamPermission.ManageTeam, principal, service, merchants, identities, cancellationToken);
        if (actor.Status != ActorStatus.Allowed || string.IsNullOrWhiteSpace(actor.Awid))
            return ActorError(actor.Status, httpContext);

        if (!TryRole(request.Role, out var role))
            return Validation(httpContext, "Merchant team role is invalid.");
        if (role == MerchantTeamRole.Owner)
            return Validation(httpContext, "Owner role is controlled by Merchant Registry.");

        try
        {
            var member = await service.ChangeRoleAsync(
                merchantId,
                RecipientDirectoryNormalization.NormalizeAfWalId(awid),
                role,
                actor.Awid,
                cancellationToken);
            return Results.Ok(ToResponse(member));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(httpContext);
        }
    }

    private static async Task<IResult> RevokeAsync(
        string merchantId,
        string awid,
        ClaimsPrincipal principal,
        MerchantTeamAccessService service,
        IMerchantRepository merchants,
        RecipientDirectoryDbContext identities,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(
            merchantId, MerchantTeamPermission.ManageTeam, principal, service, merchants, identities, cancellationToken);
        if (actor.Status != ActorStatus.Allowed || string.IsNullOrWhiteSpace(actor.Awid))
            return ActorError(actor.Status, httpContext);

        try
        {
            var member = await service.RevokeAsync(
                merchantId,
                RecipientDirectoryNormalization.NormalizeAfWalId(awid),
                actor.Awid,
                cancellationToken);
            return Results.Ok(ToResponse(member));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(httpContext);
        }
    }

    private static async Task<ActorResolution> ResolveActorAsync(
        string merchantId,
        MerchantTeamPermission permission,
        ClaimsPrincipal principal,
        MerchantTeamAccessService service,
        IMerchantRepository merchants,
        RecipientDirectoryDbContext identities,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId) || userId == Guid.Empty)
            return new(ActorStatus.Forbidden, null);

        Merchant? merchant;
        try
        {
            merchant = await merchants.GetAsync(new MerchantId(merchantId), cancellationToken);
        }
        catch (ArgumentException)
        {
            return new(ActorStatus.NotFound, null);
        }

        if (merchant is null)
            return new(ActorStatus.NotFound, null);

        var awids = await identities.AfWalIdentities
            .AsNoTracking()
            .Where(x => x.OwnerId == userId && x.IsActive)
            .Select(x => x.AfWalId)
            .ToListAsync(cancellationToken);

        if (awids.Count == 0)
            return new(ActorStatus.Forbidden, null);

        var ownerAwid = awids.FirstOrDefault(x =>
            string.Equals(x, merchant.OwnerAwid, StringComparison.OrdinalIgnoreCase));

        if (ownerAwid is not null)
        {
            await service.EnsureOwnerAsync(merchantId, ownerAwid, cancellationToken);
            return new(ActorStatus.Allowed, ownerAwid);
        }

        foreach (var awid in awids)
        {
            if (await service.IsAllowedAsync(merchantId, awid, permission, cancellationToken))
                return new(ActorStatus.Allowed, awid);
        }

        return new(ActorStatus.NotFound, null);
    }

    private static bool TryRole(string? value, out MerchantTeamRole role) =>
        Enum.TryParse(value?.Trim(), true, out role) && Enum.IsDefined(role);

    private static MerchantTeamMemberResponse ToResponse(MerchantTeamMember member)
    {
        var permissions = MerchantTeamRolePolicy.Permissions(member.Role)
            .Select(x => x.ToString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return new(
            member.Id,
            member.MerchantId,
            member.Awid,
            member.Role.ToString(),
            member.Status.ToString(),
            permissions,
            MerchantTeamRolePolicy.AllowsMoneyMovement(member.Role),
            member.CreatedAtUtc,
            member.UpdatedAtUtc);
    }

    private static IResult ActorError(ActorStatus status, HttpContext context) =>
        status == ActorStatus.Forbidden ? Forbidden(context) : NotFound(context);

    private static IResult Forbidden(HttpContext context) =>
        Results.Json(
            new MerchantTeamErrorResponse(
                MerchantTeamErrorCode.Forbidden,
                "An active personal AfWal ID with merchant access is required.",
                context.TraceIdentifier),
            statusCode: StatusCodes.Status403Forbidden);

    private static IResult NotFound(HttpContext context) =>
        Results.NotFound(new MerchantTeamErrorResponse(
            MerchantTeamErrorCode.NotFound,
            "Merchant team resource was not found.",
            context.TraceIdentifier));

    private static IResult Validation(HttpContext context, string message) =>
        Results.BadRequest(new MerchantTeamErrorResponse(
            MerchantTeamErrorCode.ValidationError,
            message,
            context.TraceIdentifier));

    private static IResult Conflict(HttpContext context, string message) =>
        Results.Conflict(new MerchantTeamErrorResponse(
            MerchantTeamErrorCode.Conflict,
            message,
            context.TraceIdentifier));

    private enum ActorStatus { Allowed = 0, Forbidden = 1, NotFound = 2 }
    private sealed record ActorResolution(ActorStatus Status, string? Awid);
}
