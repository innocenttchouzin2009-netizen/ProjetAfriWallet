using AfriWallet.Ledger.Application;

namespace IdentityService.Api.Ledger;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/ledger/journals")
            .RequireAuthorization();

        group.MapPost("/", PostJournalAsync);
        group.MapGet("/{journalEntryId:guid}", GetJournalAsync);
        group.MapGet("/by-correlation/{correlationId:guid}", GetJournalByCorrelationAsync);

        return endpoints;
    }

    private static async Task<IResult> PostJournalAsync(
        PostJournalRequest request,
        LedgerPostingApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new PostJournalCommand(
            request.CurrencyCode,
            request.BusinessReference,
            request.CorrelationId,
            request.Lines?.Select(line => new PostLedgerLineCommand(
                line.AccountId,
                line.Side,
                line.AmountMinor,
                line.Memo)).ToArray()
            ?? Array.Empty<PostLedgerLineCommand>());

        var result = await service.PostAsync(command, DateTimeOffset.UtcNow, cancellationToken);
        return ToHttpResult(
            result,
            httpContext,
            success => Results.Created($"/api/v1/ledger/journals/{success.JournalEntryId}", success));
    }

    private static async Task<IResult> GetJournalAsync(
        Guid journalEntryId,
        LedgerPostingApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(journalEntryId, cancellationToken);
        return ToHttpResult(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> GetJournalByCorrelationAsync(
        Guid correlationId,
        LedgerPostingApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByCorrelationIdAsync(correlationId, cancellationToken);
        return ToHttpResult(result, httpContext, Results.Ok);
    }

    private static IResult ToHttpResult<T>(
        LedgerOperationResult<T> result,
        HttpContext httpContext,
        Func<T, IResult> onSuccess)
    {
        if (result.Succeeded && result.Value is not null)
        {
            return onSuccess(result.Value);
        }

        var error = new LedgerErrorResponse(
            result.ErrorCode ?? LedgerErrorCode.ValidationError,
            result.ErrorMessage ?? "Ledger operation failed.",
            httpContext.TraceIdentifier);

        return result.ErrorCode switch
        {
            LedgerErrorCode.NotFound => Results.NotFound(error),
            LedgerErrorCode.DuplicateCorrelation => Results.Conflict(error),
            _ => Results.BadRequest(error)
        };
    }
}
