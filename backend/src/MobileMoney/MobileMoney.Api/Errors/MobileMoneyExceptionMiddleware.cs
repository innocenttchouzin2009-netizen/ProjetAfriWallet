using Microsoft.AspNetCore.Http;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.Errors;

public sealed class MobileMoneyExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public MobileMoneyExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            var correlationId = CorrelationContext.FromHttpContext(context)?.CorrelationId ?? CorrelationIdValidator.DefaultCorrelationId;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "MOBILE_MONEY_INTERNAL_ERROR",
                    message = "An unexpected error occurred.",
                    correlationId
                }
            });
        }
    }
}
