using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;
using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;
using MobileMoney.Production.Payout.Extensions;

var fakeResolver = new FakeResolver(
    (phoneNumber, @operator) =>
        phoneNumber == "+237670123456" && @operator == CameroonMobileOperator.Mtn
            ? new BeneficiaryAccountHolderResolution("  Ada N.  ")
            : null);

var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton<IBeneficiaryAccountHolderResolver>(fakeResolver);
builder.Services.AddMobileMoneyBeneficiaryLookup();

var app = builder.Build();
app.MapMobileMoneyBeneficiaryLookup();

var endpoint = app.DataSources
    .SelectMany(source => source.Endpoints)
    .OfType<RouteEndpoint>()
    .Single(candidate =>
        candidate.RoutePattern.RawText == MobileMoneyBeneficiaryLookupEndpointExtensions.Route);

AssertEqual(
    MobileMoneyBeneficiaryLookupEndpointExtensions.Route,
    endpoint.RoutePattern.RawText);

var httpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
Assert(httpMethods is not null, "Endpoint must expose HTTP method metadata.");
Assert(
    httpMethods!.HttpMethods.SequenceEqual(new[] { HttpMethods.Post }),
    "Beneficiary lookup endpoint must be POST-only.");

var resolved = await InvokeAsync(endpoint, app.Services, "670123456");
AssertEqual(StatusCodes.Status200OK, resolved.StatusCode);
AssertEqual("+237670123456", GetString(resolved.Json, "normalizedPhoneNumber"));
AssertEqual("CM", GetString(resolved.Json, "countryCode"));
AssertEqual("MTN", GetString(resolved.Json, "operator"));
Assert(GetBoolean(resolved.Json, "operatorResolved"), "Operator must be resolved.");
AssertEqual("Ada N.", GetString(resolved.Json, "accountHolderName"));
Assert(GetBoolean(resolved.Json, "beneficiaryResolved"), "Beneficiary holder must be resolved.");
AssertEqual(1, fakeResolver.CallCount);

var unsupported = await InvokeAsync(endpoint, app.Services, "660123456");
AssertEqual(StatusCodes.Status200OK, unsupported.StatusCode);
AssertEqual("+237660123456", GetString(unsupported.Json, "normalizedPhoneNumber"));
AssertNull(GetNullableString(unsupported.Json, "operator"));
Assert(!GetBoolean(unsupported.Json, "operatorResolved"), "Unsupported operator must remain unresolved.");
AssertNull(GetNullableString(unsupported.Json, "accountHolderName"));
Assert(!GetBoolean(unsupported.Json, "beneficiaryResolved"), "Unsupported beneficiary must remain unresolved.");
AssertEqual(1, fakeResolver.CallCount);

var invalid = await InvokeAsync(endpoint, app.Services, "67012345");
AssertEqual(StatusCodes.Status400BadRequest, invalid.StatusCode);
AssertEqual("BENEFICIARY_LOOKUP_INVALID_PHONE", GetString(invalid.Json, "code"));
AssertEqual(1, fakeResolver.CallCount);

Console.WriteLine("MobileMoney beneficiary lookup API contract scenarios: 4/4 passed.");

static async Task<(int StatusCode, JsonDocument Json)> InvokeAsync(
    RouteEndpoint endpoint,
    IServiceProvider services,
    string phoneNumber)
{
    var context = new DefaultHttpContext
    {
        RequestServices = services
    };

    context.Request.Method = HttpMethods.Post;
    context.Request.ContentType = "application/json";
    context.Request.Body = new MemoryStream(
        Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { phoneNumber })));
    context.Response.Body = new MemoryStream();

    await endpoint.RequestDelegate!(context);

    context.Response.Body.Position = 0;
    var json = await JsonDocument.ParseAsync(context.Response.Body);

    return (context.Response.StatusCode, json);
}

static string GetString(JsonDocument json, string property) =>
    json.RootElement.GetProperty(property).GetString()
    ?? throw new InvalidOperationException($"Expected '{property}' to be a string.");

static string? GetNullableString(JsonDocument json, string property)
{
    var value = json.RootElement.GetProperty(property);
    return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
}

static bool GetBoolean(JsonDocument json, string property) =>
    json.RootElement.GetProperty(property).GetBoolean();

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void AssertNull(object? value)
{
    if (value is not null)
        throw new InvalidOperationException($"Expected null, got '{value}'.");
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException(
            $"Expected '{expected}', got '{actual}'.");
}

internal sealed class FakeResolver(
    Func<string, CameroonMobileOperator, BeneficiaryAccountHolderResolution?> resolve)
    : IBeneficiaryAccountHolderResolver
{
    public int CallCount { get; private set; }

    public Task<BeneficiaryAccountHolderResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        CameroonMobileOperator @operator,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(resolve(normalizedPhoneNumber, @operator));
    }
}
