using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Application.Commands;
using AfriWallet.Merchants.Registry.Application.Services;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;
using AfriWallet.Merchants.Registry.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-merchant-registry-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";
var options = new DbContextOptionsBuilder<MerchantRegistryDbContext>()
    .UseSqlite(connectionString)
    .Options;

var now = new DateTimeOffset(2026, 9, 23, 20, 30, 0, TimeSpan.Zero);
const string actor = "merchant-registry-durability-scenario";
const string ownerAwid = "owner.durable.afwal";

BusinessProfile Profile(string tradingName) => new(
    "Durable Merchant GmbH",
    tradingName,
    MerchantType.Company,
    "DE",
    "EUR",
    "Technology",
    "HRB-12345",
    "DE123456789",
    new BusinessAddress("Musterstrasse 1", null, "Solingen", "42651", "DE"),
    new MerchantContact("merchant@example.com", "+49123456789"));

string merchantId;

try
{
    await using (var db = new MerchantRegistryDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();

        var repository = new EfMerchantRepository(db);
        var audit = new EfMerchantAuditStore(db);
        var service = new MerchantRegistryService(repository, audit, new FixedClock(now));

        var created = await service.CreateAsync(new CreateMerchantCommand(ownerAwid, Profile("Durable Store"), actor));
        merchantId = created.MerchantId;

        await service.RegisterAsync(new RegisterMerchantCommand(merchantId, actor));
        await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(merchantId, MerchantStatus.PendingVerification, actor));
        await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(merchantId, MerchantStatus.Active, actor));
        await service.SetCapabilitiesAsync(new SetMerchantCapabilitiesCommand(
            merchantId,
            [MerchantCapability.OnlinePayments, MerchantCapability.QrPayments, MerchantCapability.Payouts],
            actor));
        await service.UpdateProfileAsync(new UpdateMerchantProfileCommand(merchantId, Profile("Durable Store Updated"), actor));
    }

    await using (var db = new MerchantRegistryDbContext(options))
    {
        var repository = new EfMerchantRepository(db);
        var audit = new EfMerchantAuditStore(db);
        var service = new MerchantRegistryService(repository, audit, new FixedClock(now.AddMinutes(5)));

        var restored = await service.GetAsync(merchantId);
        Assert(restored.Status == MerchantStatus.Active, "Merchant status did not survive restart.");
        Assert(restored.Profile.TradingName == "Durable Store Updated", "Merchant profile did not survive restart.");
        Assert(restored.Capabilities.Count == 3, "Merchant capabilities did not survive restart.");

        var byOwner = await repository.GetByOwnerAwidAsync(" OWNER.DURABLE.AFWAL ");
        Assert(byOwner?.MerchantId.Value == merchantId, "Owner AWID lookup is not durable/case-insensitive.");

        Assert(await repository.ExistsByLegalNameAsync(" durable merchant gmbh ", "de"),
            "Legal-name uniqueness lookup did not survive restart.");

        var events = await audit.GetAsync(merchantId);
        Assert(events.Count >= 6, "Audit events did not survive restart.");
        Assert(events.All(x => x.Metadata["moneyMovementPerformed"] == "false"),
            "Registry audit must preserve the no-money-movement boundary.");

        var duplicateOwnerBlocked = false;
        try
        {
            await service.CreateAsync(new CreateMerchantCommand(ownerAwid, new BusinessProfile(
                "Another Durable Merchant GmbH",
                "Other Store",
                MerchantType.Company,
                "DE",
                "EUR",
                "Technology",
                null,
                null,
                new BusinessAddress("Andere Strasse 2", null, "Wuppertal", "42103", "DE"),
                new MerchantContact("other@example.com", null)), actor));
        }
        catch (InvalidOperationException)
        {
            duplicateOwnerBlocked = true;
        }
        Assert(duplicateOwnerBlocked, "Duplicate owner AWID must remain blocked after restart.");

        var duplicateLegalBlocked = false;
        try
        {
            await service.CreateAsync(new CreateMerchantCommand("another.owner.afwal", new BusinessProfile(
                "Durable Merchant GmbH",
                "Other Store",
                MerchantType.Company,
                "DE",
                "EUR",
                "Technology",
                null,
                null,
                new BusinessAddress("Andere Strasse 3", null, "Wuppertal", "42103", "DE"),
                new MerchantContact("other2@example.com", null)), actor));
        }
        catch (InvalidOperationException)
        {
            duplicateLegalBlocked = true;
        }
        Assert(duplicateLegalBlocked, "Duplicate legal name/country must remain blocked after restart.");
    }

    Console.WriteLine("AFW-BE-MERCHANT-REGISTRY-DURABILITY-1 scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class FixedClock(DateTimeOffset now) : IMerchantClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
