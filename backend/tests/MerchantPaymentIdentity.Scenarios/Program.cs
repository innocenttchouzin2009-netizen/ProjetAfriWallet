using AfriWallet.Merchants.PaymentIdentity.Application;
using AfriWallet.Merchants.PaymentIdentity.Domain;
using AfriWallet.Merchants.PaymentIdentity.Infrastructure;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static Merchant ActiveMerchant(string id, string ownerAwid, DateTimeOffset now)
{
    var profile = new BusinessProfile(
        $"Legal {id}",
        $"Trading {id}",
        MerchantType.Company,
        "CM",
        "XAF",
        "Retail",
        null,
        null,
        new BusinessAddress("1 Merchant Street", null, "Douala", "00000", "CM"),
        new MerchantContact($"{id.ToLowerInvariant()}@example.test", null));

    var merchant = new Merchant(new MerchantId(id), ownerAwid, profile, now);
    merchant.Register(now);
    merchant.BeginVerification(now);
    merchant.Activate(now);
    return merchant;
}

static Wallet ActiveWallet(Guid walletId, DateTimeOffset now) =>
    Wallet.Create(
        WalletId.From(walletId),
        Guid.NewGuid(),
        Currency.Create("XAF"),
        CountryCode.Create("CM"),
        now);

var now = new DateTimeOffset(2026, 9, 23, 20, 0, 0, TimeSpan.Zero);
var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-merchant-payment-identity-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";

var merchantOne = ActiveMerchant("AFM-IDENTITY-001", "owner.one.afwal", now);
var merchantTwo = ActiveMerchant("AFM-IDENTITY-002", "owner.two.afwal", now);
var inactiveMerchantProfile = new BusinessProfile(
    "Inactive Merchant",
    "Inactive",
    MerchantType.Company,
    "CM",
    "XAF",
    "Retail",
    null,
    null,
    new BusinessAddress("2 Merchant Street", null, "Douala", "00000", "CM"),
    new MerchantContact("inactive@example.test", null));
var inactiveMerchant = new Merchant(new MerchantId("AFM-IDENTITY-003"), "owner.three.afwal", inactiveMerchantProfile, now);

var walletOneId = Guid.NewGuid();
var walletTwoId = Guid.NewGuid();
var inactiveWalletId = Guid.NewGuid();
var walletOne = ActiveWallet(walletOneId, now);
var walletTwo = ActiveWallet(walletTwoId, now);
var inactiveWallet = ActiveWallet(inactiveWalletId, now);
inactiveWallet.Suspend(now.AddMinutes(1));

var merchants = new FakeMerchantRepository([merchantOne, merchantTwo, inactiveMerchant]);
var wallets = new FakeWalletRepository([walletOne, walletTwo, inactiveWallet]);
var clock = new FixedTimeProvider(now.AddHours(1));
Guid identityId;

try
{
    await using (var db = new MerchantPaymentIdentityDbContext(
        new DbContextOptionsBuilder<MerchantPaymentIdentityDbContext>().UseSqlite(connectionString).Options))
    {
        await db.Database.EnsureCreatedAsync();

        var registry = new EfMerchantPaymentIdentityRegistry(db);
        var audit = new EfMerchantPaymentIdentityAuditStore(db);
        var service = new MerchantPaymentIdentityService(registry, audit, merchants, wallets, clock);

        var created = await service.RegisterAsync(
            "  Shop.CM  ",
            merchantOne.MerchantId.Value,
            walletOneId,
            "operator-1");

        identityId = created.IdentityId;
        Assert(created.MerchantAfWalId == "shop.cm", "Merchant AfWal ID must be canonicalized.");
        Assert(created.MerchantId == merchantOne.MerchantId.Value, "Merchant id mismatch.");
        Assert(created.WalletId == walletOneId, "Wallet id mismatch.");
        Assert(created.Status == MerchantPaymentIdentityStatus.Active, "New merchant payment identity must be active.");

        var duplicateAliasRejected = false;
        try
        {
            await service.RegisterAsync("SHOP.CM", merchantTwo.MerchantId.Value, walletTwoId, "operator-2");
        }
        catch (InvalidOperationException)
        {
            duplicateAliasRejected = true;
        }
        Assert(duplicateAliasRejected, "Merchant AfWal ID must be globally unique inside the merchant payment identity registry.");

        var duplicateMerchantRejected = false;
        try
        {
            await service.RegisterAsync("shop-two.cm", merchantOne.MerchantId.Value, walletOneId, "operator-1");
        }
        catch (InvalidOperationException)
        {
            duplicateMerchantRejected = true;
        }
        Assert(duplicateMerchantRejected, "A merchant must not receive a second active payment identity in this delivery.");

        var inactiveMerchantRejected = false;
        try
        {
            await service.RegisterAsync("inactive-merchant.cm", inactiveMerchant.MerchantId.Value, walletTwoId, "operator-3");
        }
        catch (InvalidOperationException)
        {
            inactiveMerchantRejected = true;
        }
        Assert(inactiveMerchantRejected, "Inactive merchant must not receive a payment identity.");

        var inactiveWalletRejected = false;
        try
        {
            await service.RegisterAsync("inactive-wallet.cm", merchantTwo.MerchantId.Value, inactiveWalletId, "operator-2");
        }
        catch (InvalidOperationException)
        {
            inactiveWalletRejected = true;
        }
        Assert(inactiveWalletRejected, "Inactive wallet must not receive a merchant payment identity.");
    }

    await using (var db = new MerchantPaymentIdentityDbContext(
        new DbContextOptionsBuilder<MerchantPaymentIdentityDbContext>().UseSqlite(connectionString).Options))
    {
        var registry = new EfMerchantPaymentIdentityRegistry(db);
        var audit = new EfMerchantPaymentIdentityAuditStore(db);
        var service = new MerchantPaymentIdentityService(registry, audit, merchants, wallets, clock);

        var resolved = await service.ResolveAsync("SHOP.CM", "system:merchant-resolution");
        Assert(resolved is not null, "Merchant payment identity must resolve after database restart.");
        Assert(resolved!.MerchantId == merchantOne.MerchantId.Value, "Resolved merchant mismatch.");
        Assert(resolved.WalletId == walletOneId, "Resolved wallet mismatch.");

        await service.DisableAsync("shop.cm", "operator-1");
        var disabledResolution = await service.ResolveAsync("shop.cm", "system:merchant-resolution");
        Assert(disabledResolution is null, "Disabled merchant payment identity must not resolve.");

        var auditEntries = await service.GetAuditAsync(identityId);
        Assert(auditEntries.Count == 3, "Expected register, resolve and disable audit entries.");
        Assert(auditEntries.Count(x => x.Operation == MerchantPaymentIdentityAuditOperation.Registered) == 1, "Registration audit missing.");
        Assert(auditEntries.Count(x => x.Operation == MerchantPaymentIdentityAuditOperation.Resolved) == 1, "Resolution audit missing.");
        Assert(auditEntries.Count(x => x.Operation == MerchantPaymentIdentityAuditOperation.Disabled) == 1, "Disable audit missing.");
        Assert(auditEntries.All(x => x.MerchantAfWalId == "shop.cm"), "Audit must keep canonical Merchant AfWal ID.");
        Assert(auditEntries.All(x => x.WalletId == walletOneId), "Audit must preserve target wallet.");
    }

    Console.WriteLine("AFW-BE-MERCHANT-PAYMENT-IDENTITY-1 durable registry, resolution and audit scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

sealed class FakeMerchantRepository(IEnumerable<Merchant> seed) : IMerchantRepository
{
    private readonly Dictionary<string, Merchant> items =
        seed.ToDictionary(x => x.MerchantId.Value, StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        items.Add(merchant.MerchantId.Value, merchant);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        items[merchant.MerchantId.Value] = merchant;
        return Task.CompletedTask;
    }

    public Task<Merchant?> GetAsync(MerchantId merchantId, CancellationToken cancellationToken = default)
    {
        items.TryGetValue(merchantId.Value, out var merchant);
        return Task.FromResult(merchant);
    }

    public Task<Merchant?> GetByOwnerAwidAsync(string awid, CancellationToken cancellationToken = default) =>
        Task.FromResult(items.Values.FirstOrDefault(x => string.Equals(x.OwnerAwid, awid, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByLegalNameAsync(string legalName, string countryCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(items.Values.Any(x =>
            string.Equals(x.Profile.LegalName, legalName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Profile.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase)));
}

sealed class FakeWalletRepository(IEnumerable<Wallet> seed) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> items = seed.ToDictionary(x => x.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(items.Values.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        items.Add(wallet.Id.Value, wallet);
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        items.TryGetValue(walletId.Value, out var wallet);
        return Task.FromResult(wallet);
    }

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Wallet> result = items.Values.Where(x => x.OwnerId == ownerId).ToArray();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        items[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}
