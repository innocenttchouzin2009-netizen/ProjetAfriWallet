using MultiTenant.Application.Security;

namespace MultiTenant.Api;

public sealed class TenantResolutionMiddleware
{
    private const string TenantHeader = "X-AFW-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ITenantContextAccessor accessor)
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            await _next(httpContext);
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(TenantHeader, out var values) ||
            !Guid.TryParse(values.FirstOrDefault(), out var tenantId))
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "TENANT_CONTEXT_REQUIRED",
                $"A valid {TenantHeader} header is required.");

            return;
        }

        var subjectId = httpContext.Request.Headers["X-AFW-Subject-Id"].FirstOrDefault() ?? "sandbox-operator";
        var roles = ParseHeader(httpContext.Request.Headers["X-AFW-Roles"].FirstOrDefault());
        var permissions = ParseHeader(httpContext.Request.Headers["X-AFW-Permissions"].FirstOrDefault());

        accessor.Current = new TenantContext(tenantId, subjectId, roles, permissions);

        try
        {
            await _next(httpContext);
        }
        finally
        {
            accessor.Current = null;
        }
    }

    private static string[] ParseHeader(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string code, string detail)
    {
        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(new
        {
            code,
            message = detail,
            correlationId = context.TraceIdentifier
        });
    }
}