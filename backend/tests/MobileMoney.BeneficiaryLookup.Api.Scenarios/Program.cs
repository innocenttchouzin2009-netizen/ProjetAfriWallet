using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
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

var endpoint = ((IEndpointRouteBuilder)app).DataSources
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

app.Urls.Add("http://127.0.0.1:0");
await app.StartAsync();

try
{
    var server = app.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>()
        ?? throw new InvalidOperationException("Server addresses feature is unavailable.");
    var address = addresses.Addresses.Single();

    using var client = new HttpClient { BaseAddress = new Uri(address) };

    var resolved = await InvokeAsync(client, "670123456");
    AssertEqual(StatusCodes.Status200OK, resolved.StatusCode);
    AssertEqual("+237670123456", GetString(resolved.Json, "normalizedPhoneNumber"));
    AssertEqual("CM", GetString(resolved.Json, "countryCode"));
    AssertEqual("MTN", GetString(resolved.Json, "operator"));
    Assert(GetBoolean(resolved.Json, "operatorResolved"), "Operator must be resolved.");
    AssertEqual("Ada N.", GetString(resolved.Json, "accountHolderName"));
    Assert(GetBoolean(resolved.Json, "beneficiaryResolved"), "Beneficiary holder must be resolved.");
    AssertEqual(1, fakeResolver.CallCount);

    var unsupported = await InvokeAsync(client, "660123456");
    AssertEqual(StatusCodes.Status200OK, unsupported.StatusCode);
    AssertEqual("+237660123456", GetString(unsupported.Json, "normalizedPhoneNumber"));
    AssertNull(GetNullableString(unsupported.Json, "operator"));
    Assert(!GetBoolean(unsupported.Json, "operatorResolved"), "Unsupported operator must remain unresolved.");
    AssertNull(GetNullableString(unsupported.Json, "accountHolderName"));
    Assert(!GetBoolean(unsupported.Json, "beneficiaryResolved"), "Unsupported beneficiary must remain unresolved.");
    AssertEqual(1, fakeResolver.CallCount);

    var invalid = await InvokeAsync(client, "67012345");
    AssertEqual(StatusCodes.Status400BadRequest, invalid.StatusCode);
    AssertEqual("BENEFICIARY_LOOKUP_INVALID_PHONE", GetString(invalid.Json, "code"));
    AssertEqual(1, fakeResolver.CallCount);

    Console.WriteLine("MobileMoney beneficiary lookup API contract scenarios: 4/4 passed.");
}
finally
{
    await app.StopAsync();
}

static async Task<(int StatusCode, JsonDocument Json)> InvokeAsync(
    HttpClient client,
    string phoneNumber)
{
    using var response = await client.PostAsJsonAsync(
        MobileMoneyBeneficiaryLookupEndpointExtensions.Route,
        new { phoneNumber });

    var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return ((int)response.StatusCode, json);
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
