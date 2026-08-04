namespace UniversalWallet.Api.Infrastructure.Observability;

public sealed class HealthChecks
{
    public static void MapHealthChecks(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            service = "universal-wallet-api",
            timestamp = DateTimeOffset.UtcNow
        }));

        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

        endpoints.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
    }
}
