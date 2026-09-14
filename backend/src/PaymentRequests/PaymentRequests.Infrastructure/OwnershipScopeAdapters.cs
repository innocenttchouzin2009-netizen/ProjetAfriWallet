using AfriWallet.P2P.Directory.Persistence;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Infrastructure;

public sealed class WalletRegistryOwnedWalletReader(IWalletRepository walletRepository)
    : IPaymentRequestOwnedWalletReader
{
    public async Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var wallets = await walletRepository.ListByOwnerAsync(userId, cancellationToken);
        return wallets.Select(wallet => wallet.Id).Distinct().ToArray();
    }
}

public sealed class AuthoritativeRecipientReferenceReader(
    RecipientDirectoryDbContext recipientDirectoryDbContext,
    PaymentRequestDbContext paymentRequestDbContext)
    : IPaymentRequestOwnedRecipientReferenceReader
{
    public async Task<IReadOnlyCollection<RecipientReference>> ListOwnedRecipientReferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var afWalIds = await recipientDirectoryDbContext.AfWalIdentities
            .AsNoTracking()
            .Where(entry => entry.OwnerId == userId && entry.IsActive)
            .Select(entry => entry.AfWalId)
            .ToListAsync(cancellationToken);

        var activeQrHashes = await recipientDirectoryDbContext.QrRecipients
            .AsNoTracking()
            .Where(entry => entry.OwnerId == userId && entry.IsActive)
            .Select(entry => entry.TokenHash)
            .ToListAsync(cancellationToken);

        var result = new HashSet<RecipientReference>();
        foreach (var afWalId in afWalIds)
        {
            result.Add(RecipientReference.FromAfWalId(afWalId));
        }

        if (activeQrHashes.Count == 0)
        {
            return result.ToArray();
        }

        var ownedHashes = activeQrHashes.ToHashSet(StringComparer.Ordinal);
        var persistedQrTokens = await paymentRequestDbContext.PaymentRequests
            .AsNoTracking()
            .Where(entity => entity.PayerReferenceKind == (int)RecipientReferenceKind.QrToken)
            .Select(entity => entity.PayerReferenceValue)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var token in persistedQrTokens)
        {
            if (ownedHashes.Contains(RecipientDirectoryNormalization.HashQrToken(token)))
            {
                result.Add(RecipientReference.FromQrToken(token));
            }
        }

        return result.ToArray();
    }
}
