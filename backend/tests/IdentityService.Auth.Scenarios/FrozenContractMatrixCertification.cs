using System.Reflection;
using System.Runtime.CompilerServices;
using IdentityService.Api.Auth.Contracts;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Endpoints;

internal static class FrozenContractMatrixCertification
{
    private const string BackendFreeze = "backend-auth-dlv-auth-1 @ 12f35aa9cac09ee5f3ac4436f7964afe7c1692c9";

    [ModuleInitializer]
    internal static void Initialize()
    {
        CertifyRoutes();
        CertifyDtoShapes();
        CertifyErrorCodes();
        Console.WriteLine($"Frozen auth backend contract certified: {BackendFreeze}");
    }

    private static void CertifyRoutes()
    {
        AssertEqual("/api/v1/auth/register", AuthRoutes.Register, nameof(AuthRoutes.Register));
        AssertEqual("/api/v1/auth/login", AuthRoutes.Login, nameof(AuthRoutes.Login));
        AssertEqual("/api/v1/auth/refresh", AuthRoutes.Refresh, nameof(AuthRoutes.Refresh));
        AssertEqual("/api/v1/auth/logout", AuthRoutes.Logout, nameof(AuthRoutes.Logout));
        AssertEqual("/api/v1/auth/logout-all", AuthRoutes.LogoutAll, nameof(AuthRoutes.LogoutAll));
        AssertEqual("/api/v1/auth/session", AuthRoutes.Session, nameof(AuthRoutes.Session));
    }

    private static void CertifyDtoShapes()
    {
        AssertShape<RegisterRequest>(
            ("Identifier", typeof(string)),
            ("Password", typeof(string)));

        AssertShape<RegisterResponse>(
            ("UserId", typeof(Guid)),
            ("Status", typeof(string)));

        AssertShape<LoginRequest>(
            ("Identifier", typeof(string)),
            ("Password", typeof(string)),
            ("DeviceId", typeof(string)),
            ("Platform", typeof(string)),
            ("DeviceName", typeof(string)));

        AssertShape<AuthSessionResponse>(
            ("AccessToken", typeof(string)),
            ("RefreshToken", typeof(string)),
            ("TokenType", typeof(string)),
            ("ExpiresIn", typeof(long)),
            ("SessionId", typeof(Guid)),
            ("UserId", typeof(Guid)));

        AssertShape<RefreshRequest>(
            ("RefreshToken", typeof(string)));

        AssertShape<LogoutRequest>();

        AssertShape<CurrentSessionResponse>(
            ("UserId", typeof(Guid)),
            ("SessionId", typeof(Guid)),
            ("DeviceId", typeof(string)),
            ("CreatedAtUtc", typeof(DateTimeOffset)),
            ("ExpiresAtUtc", typeof(DateTimeOffset)),
            ("TokenVersion", typeof(long)));

        AssertShape<AuthErrorResponse>(
            ("Code", typeof(string)),
            ("Message", typeof(string)),
            ("TraceId", typeof(string)));
    }

    private static void CertifyErrorCodes()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "AUTH_INVALID_CREDENTIALS",
            "AUTH_IDENTIFIER_ALREADY_EXISTS",
            "AUTH_SESSION_EXPIRED",
            "AUTH_SESSION_REVOKED",
            "AUTH_REFRESH_INVALID",
            "AUTH_REFRESH_EXPIRED",
            "AUTH_REFRESH_REUSED",
            "AUTH_TOKEN_INVALID",
            "AUTH_TOKEN_EXPIRED",
            "AUTH_USER_DISABLED",
            "AUTH_VALIDATION_ERROR"
        };

        var actual = typeof(AuthErrorCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        if (!actual.SetEquals(expected))
        {
            throw new InvalidOperationException(
                $"Frozen backend auth error-code contract changed. Expected: {string.Join(", ", expected.Order())}. Actual: {string.Join(", ", actual.Order())}.");
        }
    }

    private static void AssertShape<T>(params (string Name, Type Type)[] expected)
    {
        var actual = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(property => property.Name, property => property.PropertyType, StringComparer.Ordinal);

        if (actual.Count != expected.Length)
        {
            throw new InvalidOperationException(
                $"{typeof(T).Name} frozen DTO field count changed. Expected {expected.Length}, actual {actual.Count}.");
        }

        foreach (var (name, type) in expected)
        {
            if (!actual.TryGetValue(name, out var actualType) || actualType != type)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name}.{name} frozen DTO contract changed. Expected type {type.Name}, actual {(actualType?.Name ?? "<missing>")}.");
            }
        }
    }

    private static void AssertEqual(string expected, string actual, string contractName)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{contractName} frozen route changed. Expected '{expected}', actual '{actual}'.");
        }
    }
}
