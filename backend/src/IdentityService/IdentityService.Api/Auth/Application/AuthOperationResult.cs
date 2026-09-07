namespace IdentityService.Api.Auth.Application;

public sealed record AuthOperationResult<T>(
    bool Succeeded,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static AuthOperationResult<T> Success(T value) => new(true, value, null, null);

    public static AuthOperationResult<T> Failure(string code, string message) =>
        new(false, default, code, message);
}
