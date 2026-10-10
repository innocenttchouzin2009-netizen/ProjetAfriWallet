using System.Net;
using System.Text.Json;

namespace MobileMoney.Production.Authentication;

public sealed class HttpAfWalSessionValidator(
    HttpClient httpClient,
    AfWalSessionValidationOptions options)
    : IAfWalSessionValidator
{
    private const string SessionPath = "/api/v1/auth/session";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<AfWalSessionValidationResult> ValidateAsync(
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.TrimStart().StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
        {
            return AfWalSessionValidationResult.Failed(
                AfWalSessionValidationStatus.MissingToken);
        }

        if (options.IdentityBaseUri is null)
        {
            return AfWalSessionValidationResult.Failed(
                AfWalSessionValidationStatus.Unavailable);
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(options.IdentityBaseUri, SessionPath));

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            authorizationHeader.Trim());

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return AfWalSessionValidationResult.Failed(
                AfWalSessionValidationStatus.Unavailable);
        }
        catch (HttpRequestException)
        {
            return AfWalSessionValidationResult.Failed(
                AfWalSessionValidationStatus.Unavailable);
        }

        using (response)
        {
            if (response.StatusCode is
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Invalid);
            }

            if ((int)response.StatusCode >= 500)
            {
                return AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Unavailable);
            }

            if (!response.IsSuccessStatusCode)
            {
                return AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Incompatible);
            }

            AfWalValidatedSession? session;
            try
            {
                session = await JsonSerializer.DeserializeAsync<AfWalValidatedSession>(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    JsonOptions,
                    cancellationToken);
            }
            catch (JsonException)
            {
                return AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Incompatible);
            }

            if (session is null ||
                session.UserId == Guid.Empty ||
                session.SessionId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.DeviceId) ||
                session.TokenVersion <= 0)
            {
                return AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Incompatible);
            }

            if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                return AfWalSessionValidationResult.Failed(
                    AfWalSessionValidationStatus.Expired);
            }

            return AfWalSessionValidationResult.Valid(session);
        }
    }
}
