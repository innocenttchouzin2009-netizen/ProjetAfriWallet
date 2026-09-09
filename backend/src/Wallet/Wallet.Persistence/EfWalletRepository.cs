using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Wallet.Persistence;

public sealed class EfWalletRepository(WalletDbContext dbContext) : IWalletRepository
{
    public Task<bool> ExistsAsync(
        Guid ownerId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        dbContext.Wallets.AnyAsync(
            wallet => wallet.OwnerId == ownerId && wallet.CurrencyCode == currencyCode,
            cancellationToken);

    public async Task AddAsync(Domain.Wallet wallet, CancellationToken cancellationToken = default)
    {
        dbContext.Wallets.Add(ToEntity(wallet));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Domain.Wallet?> GetAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Wallets
            .AsNoTracking()
            .SingleOrDefaultAsync(wallet => wallet.Id == walletId.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Domain.Wallet>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.OwnerId == ownerId)
            .OrderBy(wallet => wallet.CreatedAtUtc)
            .ThenBy(wallet => wallet.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public async Task UpdateAsync(Domain.Wallet wallet, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Wallets
            .SingleOrDefaultAsync(candidate => candidate.Id == wallet.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException("Wallet persistence record was not found.");

        entity.Status = (int)wallet.Status;
        entity.UpdatedAtUtc = wallet.UpdatedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WalletEntity ToEntity(Domain.Wallet wallet) => new()
    {
        Id = wallet.Id.Value,
        OwnerId = wallet.OwnerId,
        CurrencyCode = wallet.Currency.Code,
        CountryCode = wallet.CountryCode?.Value,
        Status = (int)wallet.Status,
        CreatedAtUtc = wallet.CreatedAtUtc,
        UpdatedAtUtc = wallet.UpdatedAtUtc
    };

    private static Domain.Wallet ToDomain(WalletEntity entity) =>
        Domain.Wallet.Restore(
            WalletId.From(entity.Id),
            entity.OwnerId,
            Currency.Create(entity.CurrencyCode),
            entity.CountryCode is null ? null : CountryCode.Create(entity.CountryCode),
            (WalletStatus)entity.Status,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}
