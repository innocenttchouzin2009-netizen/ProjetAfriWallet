namespace IdentityService.Api.Identity;

public static class PublicAfWalIdEndpoints
{
    public const string Route = "/api/v1/identity/afwal-id/{afWalId}";

    public static IEndpointRouteBuilder MapPublicAfWalIdEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(Route, GetAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        string afWalId,
        PublicAfWalIdReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ReadAsync(afWalId, cancellationToken);
        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }
}
