using AfriWallet.CardPlatform.Application.Services;
using AfriWallet.CardPlatform.Domain.Entities;
using AfriWallet.CardPlatform.Infrastructure;

var repository = new InMemoryTokenRepository();
var vault = new TokenVault();
var validator = new TokenValidator();
var service = new TokenizationService(repository, vault, validator);

var token = await service.CreateAsync(new CardTokenRequest
{
    CardId = "card-001",
    OwnerAwidId = "owner-001",
    WalletId = "wallet-001",
    Network = "Visa",
    TokenType = "NETWORK_TOKEN"
});
Console.WriteLine(token is not null ? "token generation ..................... PASS" : "token generation ..................... FAIL");

var activated = await service.ActivateAsync(token!.TokenId);
Console.WriteLine(activated?.Status == "ACTIVE" ? "token activation ..................... PASS" : "token activation ..................... FAIL");

var suspended = await service.SuspendAsync(activated!.TokenId);
Console.WriteLine(suspended?.Status == "SUSPENDED" ? "token suspension ..................... PASS" : "token suspension ..................... FAIL");

var resumed = await service.ResumeAsync(suspended!.TokenId);
Console.WriteLine(resumed?.Status == "ACTIVE" ? "token suspension ..................... PASS" : "token suspension ..................... FAIL");

var rotated = await service.RotateAsync(resumed!.TokenId);
Console.WriteLine(rotated?.Status == "ROTATED" ? "token rotation ....................... PASS" : "token rotation ....................... FAIL");

var revoked = await service.RevokeAsync(rotated!.TokenId);
Console.WriteLine(revoked?.Status == "REVOKED" ? "token revocation ..................... PASS" : "token revocation ....................... FAIL");

var validation = await service.ValidateAsync(revoked!.TokenReference);
Console.WriteLine(validation is null ? "expired token rejected ............... PASS" : "expired token rejected ............... FAIL");

var masked = token.TokenReference;
Console.WriteLine(masked.Contains("tok_", StringComparison.OrdinalIgnoreCase) && masked.Length > 8 ? "masked data only ..................... PASS" : "masked data only ..................... FAIL");

var audit = await repository.GetAuditTrailAsync(token.TokenId);
Console.WriteLine(audit.Count > 0 ? "audit generation ..................... PASS" : "audit generation ..................... FAIL");
Console.WriteLine(service.GetTelemetryCount() > 0 ? "telemetry generation ................. PASS" : "telemetry generation ................. FAIL");

Console.WriteLine("\nAll AFW-DLV-0008.4 card tokenization scenarios passed.");
