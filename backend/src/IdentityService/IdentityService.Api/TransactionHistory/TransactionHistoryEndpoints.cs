using System.Security.Claims;
using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionHistory.Application;
using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Cursor;
using AfriWallet.TransactionHistory.Application.Projection;
using AfriWallet.TransactionHistory.Infrastructure;

namespace IdentityService.Api.TransactionHistory;

public static class TransactionHistoryEndpoints
{
    public static IServiceCollection AddTransactionHistoryHttpAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var mappings = LoadWalletLedgerAccountMappings(configuration);

        services.AddSingleton<ITransactionHistoryLedgerAccountResolver>(
            new ConfiguredTransactionHistoryLedgerAccountResolver(mappings));
        services.AddScoped<ITransactionHistoryOwnedWalletReader, WalletRepositoryTransactionHistoryOwnedWalletReader>();
        services.AddScoped<ITransactionHistoryReader, LedgerBackedTransactionHistoryReader>();
        services.AddSingleton<TransactionHistoryProjector>();
        services.AddScoped<AuthorizedTransactionHistoryQueryService>();

        return services;
    }

    public static IEndpointRouteBuilder MapTransactionHistoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/transactions")
            .RequireAuthorization()
            .MapGet("/", ListAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        AuthorizedTransactionHistoryQueryService service,
        HttpContext httpContext,
        int? limit,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new TransactionHistoryErrorResponse(
                    TransactionHistoryHttpErrorCode.Unauthorized,
                    "Authenticated user id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var decodedCursor = TransactionHistoryCursorCodec.Decode(cursor);
            var pageRequest = TransactionHistoryPageRequest.Create(
                limit ?? TransactionHistoryPageRequest.DefaultLimit,
                decodedCursor);

            var page = await service.ListAsync(
                new TransactionHistoryQuery(userId, pageRequest),
                cancellationToken);

            return Results.Ok(new TransactionHistoryPageResponse(
                page.Items.Select(ToHttpItem).ToArray(),
                page.NextCursor is { } next
                    ? TransactionHistoryCursorCodec.Encode(next)
                    : null));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new TransactionHistoryErrorResponse(
                TransactionHistoryHttpErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static TransactionHistoryItemResponse ToHttpItem(TransactionHistoryItem item) =>
        new(
            item.TransactionId,
            item.WalletId.Value,
            item.AmountMinor,
            item.CurrencyCode,
            item.Direction.ToString(),
            item.Status.ToString(),
            item.OccurredAtUtc,
            item.Reference,
            item.CounterpartyLabel);

    private static IReadOnlyDictionary<Guid, AccountId> LoadWalletLedgerAccountMappings(
        IConfiguration configuration)
    {
        var mappings = new Dictionary<Guid, AccountId>();

        foreach (var child in configuration.GetSection("Transfer:WalletLedgerAccounts").GetChildren())
        {
            if (!Guid.TryParse(child.Key, out var walletId) ||
                !Guid.TryParse(child.Value, out var accountGuid))
            {
                throw new InvalidOperationException(
                    "Transaction history wallet-ledger account mappings must use GUID wallet keys and GUID account values.");
            }

            mappings[walletId] = new AccountId(accountGuid);
        }

        return mappings;
    }
}
