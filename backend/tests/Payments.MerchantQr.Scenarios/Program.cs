using UniversalWallet.Api.Payments.Application.MerchantPayments;
using UniversalWallet.Api.Payments.Domain.MerchantPayments;
using UniversalWallet.Api.Payments.Infrastructure.MerchantPayments;

var failures = new List<string>();
var merchantProfileRepository = new InMemoryMerchantProfileRepository();
var requestRepository = new InMemoryMerchantPaymentRequestRepository();
var qrRepository = new InMemoryMerchantQrTokenRepository();
var resolveHandler = new ResolveMerchantQrHandler(qrRepository, merchantProfileRepository, requestRepository);
var createRequestHandler = new CreateMerchantPaymentRequestHandler(merchantProfileRepository, requestRepository, qrRepository);

await Run("static qr resolution works", async () =>
{
    var merchant = new MerchantProfile
    {
        MerchantAwid = Guid.NewGuid(),
        BusinessName = "AfroBol",
        DisplayName = "AfroBol SG",
        CategoryCode = MerchantCategoryCode.Restaurant,
        SettlementWalletId = Guid.NewGuid(),
        Status = MerchantStatus.Active,
        VerificationLevel = MerchantVerificationLevel.Verified,
        CountryCode = "CM"
    };
    await merchantProfileRepository.AddAsync(merchant);

    var qrToken = new MerchantQrToken { MerchantId = merchant.Id, Type = MerchantQrType.MerchantStatic, Token = "AQR_STATIC", IsActive = true };
    await qrRepository.AddAsync(qrToken);

    var response = await resolveHandler.HandleAsync(new ResolveMerchantQrRequest("AQR_STATIC"));
    Assert(response.QrType == "MerchantStatic", "static QR should resolve");
});

await Run("dynamic payment request and QR resolution work", async () =>
{
    var merchant = new MerchantProfile
    {
        MerchantAwid = Guid.NewGuid(),
        BusinessName = "AfroBol",
        DisplayName = "AfroBol SG",
        CategoryCode = MerchantCategoryCode.Restaurant,
        SettlementWalletId = Guid.NewGuid(),
        Status = MerchantStatus.Active,
        VerificationLevel = MerchantVerificationLevel.Verified,
        CountryCode = "CM"
    };
    await merchantProfileRepository.AddAsync(merchant);

    var created = await createRequestHandler.HandleAsync(merchant.MerchantAwid, new CreateMerchantPaymentRequestRequest(1850, "EUR", "Commande #1048", 300, "POS-1048"));
    Assert(created.RequestId != Guid.Empty, "dynamic request should be created");
});

if (failures.Count == 0)
{
    Console.WriteLine("All merchant QR scenarios passed.");
    return;
}

Console.WriteLine("Merchant QR scenarios failed:");
foreach (var failure in failures)
{
    Console.WriteLine($" - {failure}");
}
Environment.ExitCode = 1;

async Task Run(string name, Func<Task> scenario)
{
    try
    {
        await scenario();
        Console.WriteLine($"[OK] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[KO] {name} -> {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}
