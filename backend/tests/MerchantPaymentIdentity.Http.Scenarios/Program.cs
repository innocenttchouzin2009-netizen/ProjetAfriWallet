using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Merchants.PaymentIdentity.Application;
using IdentityService.Api.MerchantPaymentIdentity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var walletId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
var resolver = new StubResolver(walletId);
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IMerchantPaymentIdentityResolver>(resolver);
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization(); app.MapMerchantPaymentIdentityEndpoints(); await app.StartAsync();
var anonymous = app.GetTestClient();
var authenticated = app.GetTestClient();
authenticated.DefaultRequestHeaders.Add("X-Test-User", "11111111-2222-3333-4444-555555555555");

await Run("authentication required", async () => Assert((await anonymous.GetAsync("/api/v1/merchant-payment-identities/shop.afwal/resolve")).StatusCode == HttpStatusCode.Unauthorized, "Expected 401."));
await Run("active identity resolves", async () => {
    var response = await authenticated.GetAsync("/api/v1/merchant-payment-identities/shop.afwal/resolve");
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var body = await response.Content.ReadFromJsonAsync<MerchantPaymentIdentityResolutionResponse>();
    Assert(body is not null && body.MerchantAfWalId == "shop.afwal" && body.MerchantId == "AFM-PAYMENT0001" && body.WalletId == walletId, "Resolution mismatch.");
    Assert(resolver.LastActor == "user:11111111-2222-3333-4444-555555555555", "Audited actor mismatch.");
});
await Run("unknown identity hidden", async () => Assert((await authenticated.GetAsync("/api/v1/merchant-payment-identities/missing.afwal/resolve")).StatusCode == HttpStatusCode.NotFound, "Expected 404."));
Console.WriteLine("AFW-BE-MERCHANT-PAYMENT-IDENTITY-API-1 protected HTTP scenarios: PASS");

static async Task Run(string name, Func<Task> scenario) { await scenario(); Console.WriteLine($"PASS: {name}"); }
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

sealed class StubResolver(Guid walletId) : IMerchantPaymentIdentityResolver {
 public string? LastActor { get; private set; }
 public Task<MerchantPaymentIdentityResolution?> ResolveAsync(string merchantAfWalId, string actor, CancellationToken cancellationToken = default) {
   LastActor=actor;
   MerchantPaymentIdentityResolution? r=string.Equals(merchantAfWalId,"shop.afwal",StringComparison.OrdinalIgnoreCase)?new("shop.afwal","AFM-PAYMENT0001",walletId):null;
   return Task.FromResult(r);
 }
}
sealed class HeaderAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder) {
 protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
   if(!Request.Headers.TryGetValue("X-Test-User",out var raw)) return Task.FromResult(AuthenticateResult.NoResult());
   var identity=new ClaimsIdentity([new Claim("sub",raw.ToString())],Scheme.Name);
   return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity),Scheme.Name)));
 }
}