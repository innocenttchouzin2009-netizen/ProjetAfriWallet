using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.Authentication;

public static class MobileMoneySessionGuardExtensions
{
    public const string SessionRequiredCode = "AUTH_SESSION_REQUIRED";
    public const string SessionInvalidCode = "AUTH_SESSION_INVALID";
    public const string SessionExpiredCode = "AUTH_SESSION_EXPIRED";
    public const string SessionIncompatibleCode = "AUTH_SESSION_INCOMPATIBLE";
    public const string SessionUnavailableCode = "AUTH_SESSION_UNAVAILABLE";

    public static RouteHandlerBuilder RequireAfWalSession(
        this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddEndpointFilter(
            async (invocationContext, next) =>
            {
                var httpContext = invocationContext.HttpContext;
                var validator = httpContext.RequestServices
                    .GetRequiredService<IAfWalSessionValidator>();

                var denied = await AuthorizeAsync(
                    httpContext,
                    validator,
                    httpContext.RequestAborted);

                return denied ?? await next(invocationContext);
            });
    }

    public static async Task<IResult?> AuthorizeAsync(
        HttpContext httpContext,
        IAfWalSessionValidator validator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(validator);

        var validation = await validator.ValidateAsync(
            httpContext.Request.Headers["Authorization"].ToString(),
            cancellationToken);

        if (validation.Succeeded)
        {
            httpContext.Items[AfWalValidatedSession.HttpContextItemKey] =
                validation.Session!;

            return null;
        }

        var (statusCode, code, message) = validation.Status switch
        {
            AfWalSessionValidationStatus.MissingToken =>
                (
                    StatusCodes.Status401Unauthorized,
                    SessionRequiredCode,
                    "An authenticated AfWal session is required."
                ),
            AfWalSessionValidationStatus.Expired =>
                (
                    StatusCodes.Status401Unauthorized,
                    SessionExpiredCode,
                    "The AfWal session has expired."
                ),
            AfWalSessionValidationStatus.Incompatible =>
                (
                    StatusCodes.Status401Unauthorized,
                    SessionIncompatibleCode,
                    "The AfWal session is incompatible with this request."
                ),
            AfWalSessionValidationStatus.Unavailable =>
                (
                    StatusCodes.Status503ServiceUnavailable,
                    SessionUnavailableCode,
                    "AfWal session validation is temporarily unavailable."
                ),
            _ =>
                (
                    StatusCodes.Status401Unauthorized,
                    SessionInvalidCode,
                    "The AfWal session is invalid or revoked."
                )
        };

        var correlationId =
            CorrelationContext.FromHttpContext(httpContext)?.CorrelationId ??
            httpContext.TraceIdentifier;

        return Results.Json(
            new MobileMoneySessionErrorResponse(
                code,
                message,
                correlationId),
            statusCode: statusCode);
    }
}

public sealed record MobileMoneySessionErrorResponse(
    string Code,
    string Message,
    string? CorrelationId);
