using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MobileMoney.Production.Auth;

public sealed class HttpAfWalSessionValidator(HttpClient httpClient)
    : IAfWalSessionValidator
{
    private const string SessionPath = "/api/v1/auth/session";

    public async Task<AfWalSessionValidationResult> ValidateAsync(
        string accessToken,
        Guid userId,
        Guid sessionId,
        long tokenVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken) ||
            userId == Guid.Empty ||
            sessionId == Guid.Empty ||
            tokenVersion < 0)
        {
            return AfWalSessionValidationResult.Failure(
                AfWalSessionValidationErrorCode.TokenInvalid,
                "Invalid access token claims.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            SessionPath);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden)
            {
                var remoteError = await TryReadErrorAsync(
                    response,
                    cancellationToken);

                return AfWalSessionValidationResult.Failure(
                    remoteError?.Code ??
                        AfWalSessionValidationErrorCode.TokenInvalid,
                    remoteError?.Message ??
                        "AfWal session validation was rejected.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return AfWalSessionValidationResult.Failure(
                    AfWalSessionValidationErrorCode.SessionUnavailable,
                    "AfWal session validation is temporarily unavailable.");
            }

            AfWalCurrentSessionResponse? session;
            try
            {
                session = await response.Content
                    .ReadFromJsonAsync<AfWalCurrentSessionResponse>(
                        cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                return AfWalSessionValidationResult.Failure(
                    AfWalSessionValidationErrorCode.SessionUnavailable,
                    "AfWal session validation returned an invalid response.");
            }

            if (session is null)
            {
                return AfWalSessionValidationResult.Failure(
                    AfWalSessionValidationErrorCode.SessionUnavailable,
                    "AfWal session validation returned an empty response.");
            }

            if (session.UserId != userId ||
                session.SessionId != sessionId ||
                session.TokenVersion != tokenVersion)
            {
                return AfWalSessionValidationResult.Failure(
                    AfWalSessionValidationErrorCode.TokenInvalid,
                    "AfWal session does not match the access token.");
            }

            if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                return AfWalSessionValidationResult.Failure(
                    AfWalSessionValidationErrorCode.SessionExpired,
                    "AfWal session expired.");
            }

            return AfWalSessionValidationResult.Success();
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return AfWalSessionValidationResult.Failure(
                AfWalSessionValidationErrorCode.SessionUnavailable,
                "AfWal session validation timed out.");
        }
        catch (HttpRequestException)
        {
            return AfWalSessionValidationResult.Failure(
                AfWalSessionValidationErrorCode.SessionUnavailable,
                "AfWal session validation is temporarily unavailable.");
        }
    }

    private static async Task<AfWalAuthErrorResponse?> TryReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content
                .ReadFromJsonAsync<AfWalAuthErrorResponse>(
                    cancellationToken: cancellationToken);
        }
        catch (Exception exception)
            when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed record AfWalCurrentSessionResponse(
        Guid UserId,
        Guid SessionId,
        string DeviceId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        long TokenVersion);

    private sealed record AfWalAuthErrorResponse(
        string Code,
        string Message,
        string? TraceId);
}
