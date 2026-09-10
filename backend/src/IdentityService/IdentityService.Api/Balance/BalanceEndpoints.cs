using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;

namespace IdentityService.Api.Balance;

public static class BalanceEndpoints
{
    public static IEndpointRouteBuilder MapBalanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/balances")
            .RequireAuthorization();

        group.MapGet("/{accountId:guid}/{currencyCode}", GetBalanceAsync);

        return endpoints;
    }

    private static async Task<IResult> GetBalanceAsync(
        Guid accountId,
        string currencyCode,
        LedgerBackedBalanceReadService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        BalanceKey key;
        try
        {
            key = new BalanceKey(new AccountId(accountId), currencyCode);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new BalanceErrorResponse(
                "BALANCE_VALIDATION_ERROR",
                exception.Message,
                httpContext.TraceIdentifier));
        }

        var snapshot = await service.ReadAsync(key, cancellationToken);
        return Results.Ok(new BalanceResponse(
            snapshot.Key.AccountId.Value,
            snapshot.Key.CurrencyCode,
            snapshot.DebitMinor,
            snapshot.CreditMinor,
            snapshot.NetMinor));
    }
}
