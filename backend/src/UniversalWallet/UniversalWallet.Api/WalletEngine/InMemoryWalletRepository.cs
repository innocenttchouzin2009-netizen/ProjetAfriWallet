using System.Security.Cryptography;

namespace UniversalWallet.Api.WalletEngine;

public interface IWalletRepository
{
    Wallet Create(string awid, WalletType walletType, string currency);
    IReadOnlyList<Wallet> ListByAwid(string awid);
    Wallet? GetById(Guid id);
    Wallet? UpdateStatus(Guid id, WalletStatus status);
    IReadOnlyList<LedgerEntry> GetLedger(Guid walletId);
    IReadOnlyList<string> SupportedCurrencies();
}

public sealed class InMemoryWalletRepository : IWalletRepository
{
    private static readonly string[] CurrencyList = ["EUR", "USD", "XAF", "XOF", "GBP", "CAD", "CHF", "NGN", "KES", "GHS"];

    private readonly object _sync = new();
    private readonly Dictionary<Guid, Wallet> _wallets = new();
    private readonly Dictionary<Guid, List<LedgerEntry>> _ledger = new();

    public Wallet Create(string awid, WalletType walletType, string currency)
    {
        lock (_sync)
        {
            var normalizedCurrency = currency.Trim().ToUpperInvariant();
            if (!CurrencyList.Contains(normalizedCurrency))
            {
                throw new InvalidOperationException("CURRENCY_NOT_SUPPORTED");
            }

            if (_wallets.Values.Any(x => x.AwidId == ParseAwid(awid) && x.Currency == normalizedCurrency && x.WalletType == walletType && x.Status != WalletStatus.Closed))
            {
                throw new InvalidOperationException("WALLET_ALREADY_EXISTS");
            }

            var wallet = new Wallet
            {
                AwidId = ParseAwid(awid),
                WalletNumber = BuildWalletNumber(normalizedCurrency),
                WalletType = walletType,
                Currency = normalizedCurrency,
                Status = WalletStatus.Created,
                AvailableBalance = 0m,
                PendingBalance = 0m,
                ReservedBalance = 0m
            };

            _wallets[wallet.Id] = wallet;
            _ledger[wallet.Id] = [];

            return wallet;
        }
    }

    public IReadOnlyList<Wallet> ListByAwid(string awid)
    {
        lock (_sync)
        {
            var awidId = ParseAwid(awid);
            return _wallets.Values.Where(x => x.AwidId == awidId).OrderBy(x => x.Currency).ThenBy(x => x.WalletType).ToList();
        }
    }

    public Wallet? GetById(Guid id)
    {
        lock (_sync)
        {
            return _wallets.GetValueOrDefault(id);
        }
    }

    public Wallet? UpdateStatus(Guid id, WalletStatus status)
    {
        lock (_sync)
        {
            if (!_wallets.TryGetValue(id, out var wallet))
            {
                return null;
            }

            if (wallet.Status == WalletStatus.Closed)
            {
                throw new InvalidOperationException("WALLET_CLOSED");
            }

            wallet.Status = status;
            wallet.UpdatedAt = DateTimeOffset.UtcNow;
            _wallets[id] = wallet;
            return wallet;
        }
    }

    public IReadOnlyList<LedgerEntry> GetLedger(Guid walletId)
    {
        lock (_sync)
        {
            return _ledger.GetValueOrDefault(walletId, []).OrderByDescending(x => x.CreatedAt).ToList();
        }
    }

    public IReadOnlyList<string> SupportedCurrencies() => CurrencyList;

    private static string BuildWalletNumber(string currency)
    {
        const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        Span<char> random = stackalloc char[6];
        for (var i = 0; i < random.Length; i++)
        {
            random[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return $"AFW-{currency}-{new string(random)}";
    }

    private static Guid ParseAwid(string awid)
    {
        // Deterministic but opaque keying from AWID string.
        var bytes = System.Text.Encoding.UTF8.GetBytes(awid.Trim().ToUpperInvariant());
        var hash = SHA256.HashData(bytes);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
