using System.Security.Cryptography;
using System.Text;

namespace Subscriptions.Api;

public sealed class TechnicalKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedKey;

    public TechnicalKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _expectedKey = configuration["TechnicalKey"] ?? "dev-key";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/internal", StringComparison.OrdinalIgnoreCase))
        {
            var provided = context.Request.Headers["X-Technical-Key"].ToString();
            if (!IsValid(provided))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                return;
            }
        }

        await _next(context);
    }

    private bool IsValid(string provided)
    {
        if (string.IsNullOrWhiteSpace(provided) || string.IsNullOrWhiteSpace(_expectedKey))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(_expectedKey));
    }
}
