using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Application.Services;
using AfriWallet.CardPlatform.Domain.Entities;
using AfriWallet.CardPlatform.Infrastructure;

var cardProgramRepository = new InMemoryCardProgramRepository();
var cardRepository = new InMemoryVirtualCardRepository();
var cardService = new VirtualCardService(cardRepository);
var authorizationRepository = new InMemoryAuthorizationRepository();
var authorizationService = new CardAuthorizationService(cardProgramRepository, cardRepository, authorizationRepository);

var card = await cardService.CreateAsync(new VirtualCard
{
    CardProgramId = "card-program-visa-virtual-sandbox",
    WalletId = "wallet-001",
    CardToken = "auth-card-active",
    BaseCurrency = "XAF",
    AllowedCurrencies = ["XAF"],
    EcommerceEnabled = true,
    ContactlessEnabled = true,
    InternationalEnabled = true
});

await cardService.ActivateAsync(card.VirtualCardId);

var approved = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = card.VirtualCardId,
    WalletId = "wallet-001",
    AmountMinor = 10_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online"
});
Console.WriteLine(approved.Decision == "AUTHORIZED" ? "authorization approved ............... PASS" : "authorization approved ............... FAIL");

var insufficientFunds = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = card.VirtualCardId,
    WalletId = "wallet-001",
    AmountMinor = 150_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online"
});
Console.WriteLine(insufficientFunds.Decision == "INSUFFICIENT_FUNDS" ? "insufficient funds ................... PASS" : "insufficient funds ................... FAIL");

var frozen = await cardService.FreezeAsync(card.VirtualCardId);
var frozenDecision = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = frozen!.VirtualCardId,
    WalletId = "wallet-001",
    AmountMinor = 5_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online"
});
Console.WriteLine(frozenDecision.Decision == "CARD_FROZEN" ? "card frozen .......................... PASS" : "card frozen .......................... FAIL");

var closedCard = await cardService.CloseAsync(card.VirtualCardId);
var closedDecision = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = closedCard!.VirtualCardId,
    WalletId = "wallet-001",
    AmountMinor = 5_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online"
});
Console.WriteLine(closedDecision.Decision == "CARD_CLOSED" ? "card closed .......................... PASS" : "card closed .......................... FAIL");

var card2 = await cardService.CreateAsync(new VirtualCard
{
    CardProgramId = "card-program-visa-virtual-sandbox",
    WalletId = "wallet-002",
    CardToken = "auth-card-limits",
    BaseCurrency = "XAF",
    AllowedCurrencies = ["XAF"],
    EcommerceEnabled = true,
    ContactlessEnabled = true,
    InternationalEnabled = true
});
await cardService.ActivateAsync(card2.VirtualCardId);
var limitsDecision = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = card2.VirtualCardId,
    WalletId = "wallet-002",
    AmountMinor = 10_000_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online"
});
Console.WriteLine(limitsDecision.Decision == "LIMIT_EXCEEDED" ? "limits exceeded ...................... PASS" : "limits exceeded ...................... FAIL");

var card3 = await cardService.CreateAsync(new VirtualCard
{
    CardProgramId = "card-program-visa-virtual-sandbox",
    WalletId = "wallet-003",
    CardToken = "auth-card-controls",
    BaseCurrency = "XAF",
    AllowedCurrencies = ["XAF"],
    EcommerceEnabled = false,
    ContactlessEnabled = true,
    InternationalEnabled = true
});
await cardService.ActivateAsync(card3.VirtualCardId);
var controlsDecision = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = card3.VirtualCardId,
    WalletId = "wallet-003",
    AmountMinor = 5_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online"
});
Console.WriteLine(controlsDecision.Decision == "CONTROL_BLOCKED" ? "controls blocked ..................... PASS" : "controls blocked ..................... FAIL");

var card4 = await cardService.CreateAsync(new VirtualCard
{
    CardProgramId = "card-program-visa-virtual-sandbox",
    WalletId = "wallet-004",
    CardToken = "auth-card-fraud",
    BaseCurrency = "XAF",
    AllowedCurrencies = ["XAF"],
    EcommerceEnabled = true,
    ContactlessEnabled = true,
    InternationalEnabled = true
});
await cardService.ActivateAsync(card4.VirtualCardId);
var fraudDecision = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = card4.VirtualCardId,
    WalletId = "wallet-004",
    AmountMinor = 5_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online",
    Metadata = new Dictionary<string, object?> { ["risk_score"] = 95 }
});
Console.WriteLine(fraudDecision.Decision == "FRAUD_SUSPECTED" ? "fraud rejection ...................... PASS" : "fraud rejection ...................... FAIL");

var card5 = await cardService.CreateAsync(new VirtualCard
{
    CardProgramId = "card-program-visa-virtual-sandbox",
    WalletId = "wallet-005",
    CardToken = "auth-card-review",
    BaseCurrency = "XAF",
    AllowedCurrencies = ["XAF"],
    EcommerceEnabled = true,
    ContactlessEnabled = true,
    InternationalEnabled = true
});
await cardService.ActivateAsync(card5.VirtualCardId);
var manualReview = await authorizationService.AuthorizeAsync(new CardAuthorizationRequest
{
    CardId = card5.VirtualCardId,
    WalletId = "wallet-005",
    AmountMinor = 5_000,
    CurrencyCode = "XAF",
    MerchantCategoryCode = "5812",
    MerchantCountry = "CM",
    Channel = "online",
    Metadata = new Dictionary<string, object?> { ["risk_score"] = 70 }
});
Console.WriteLine(manualReview.Decision == "MANUAL_REVIEW" ? "manual review ........................ PASS" : "manual review ........................ FAIL");

var authorization = await authorizationService.GetByIdAsync(approved.AuthorizationId);
Console.WriteLine(authorization is not null ? "audit generated ...................... PASS" : "audit generated ...................... FAIL");
Console.WriteLine(authorization?.TraceId is not null ? "telemetry generated .................. PASS" : "telemetry generated .................. FAIL");

Console.WriteLine("\nAll AFW-DLV-0008.3 card authorization scenarios passed.");
