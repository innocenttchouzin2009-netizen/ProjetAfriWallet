using AfriWallet.Wallet.Domain;

namespace AfriWallet.Wallet.Application;

public sealed class WalletRegistryApplicationService(
    IWalletRepository repository,
    ISupportedCurrencyPolicy supportedCurrencyPolicy)
{
    public async Task<WalletOperationResult<WalletView>> CreateAsync(
        CreateWalletCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (command.OwnerId == Guid.Empty)
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.ValidationError, "Owner id is required.");
        }

        Currency currency;
        CountryCode? countryCode = null;
        try
        {
            currency = Currency.Create(command.CurrencyCode);
            if (!string.IsNullOrWhiteSpace(command.CountryCode))
            {
                countryCode = CountryCode.Create(command.CountryCode);
            }
        }
        catch (ArgumentException ex)
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.ValidationError, ex.Message);
        }

        if (!supportedCurrencyPolicy.IsSupported(currency.Code))
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.UnsupportedCurrency, "Currency is not supported.");
        }

        if (await repository.ExistsAsync(command.OwnerId, currency.Code, cancellationToken))
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.DuplicateWallet, "Owner already has a wallet in this currency.");
        }

        var wallet = Domain.Wallet.Create(WalletId.New(), command.OwnerId, currency, countryCode, nowUtc);
        await repository.AddAsync(wallet, cancellationToken);
        return WalletOperationResult<WalletView>.Success(ToView(wallet));
    }

    public async Task<WalletOperationResult<WalletView>> GetAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        if (walletId == Guid.Empty)
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.ValidationError, "Wallet id is required.");
        }

        var wallet = await repository.GetAsync(WalletId.From(walletId), cancellationToken);
        return wallet is null
            ? WalletOperationResult<WalletView>.Failure(WalletErrorCode.NotFound, "Wallet not found.")
            : WalletOperationResult<WalletView>.Success(ToView(wallet));
    }

    public async Task<WalletOperationResult<IReadOnlyList<WalletView>>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            return WalletOperationResult<IReadOnlyList<WalletView>>.Failure(WalletErrorCode.ValidationError, "Owner id is required.");
        }

        var wallets = await repository.ListByOwnerAsync(ownerId, cancellationToken);
        return WalletOperationResult<IReadOnlyList<WalletView>>.Success(wallets.Select(ToView).ToArray());
    }

    public Task<WalletOperationResult<WalletView>> SuspendAsync(Guid walletId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken = default) =>
        TransitionAsync(walletId, wallet => wallet.Suspend(changedAtUtc), cancellationToken);

    public Task<WalletOperationResult<WalletView>> ActivateAsync(Guid walletId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken = default) =>
        TransitionAsync(walletId, wallet => wallet.Activate(changedAtUtc), cancellationToken);

    public Task<WalletOperationResult<WalletView>> CloseAsync(Guid walletId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken = default) =>
        TransitionAsync(walletId, wallet => wallet.Close(changedAtUtc), cancellationToken);

    private async Task<WalletOperationResult<WalletView>> TransitionAsync(
        Guid walletId,
        Action<Domain.Wallet> transition,
        CancellationToken cancellationToken)
    {
        if (walletId == Guid.Empty)
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.ValidationError, "Wallet id is required.");
        }

        var wallet = await repository.GetAsync(WalletId.From(walletId), cancellationToken);
        if (wallet is null)
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.NotFound, "Wallet not found.");
        }

        try
        {
            transition(wallet);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return WalletOperationResult<WalletView>.Failure(WalletErrorCode.InvalidTransition, ex.Message);
        }

        await repository.UpdateAsync(wallet, cancellationToken);
        return WalletOperationResult<WalletView>.Success(ToView(wallet));
    }

    private static WalletView ToView(Domain.Wallet wallet) =>
        new(
            wallet.Id.Value,
            wallet.OwnerId,
            wallet.Currency.Code,
            wallet.CountryCode?.Value,
            wallet.Status,
            wallet.CreatedAtUtc,
            wallet.UpdatedAtUtc);
}
