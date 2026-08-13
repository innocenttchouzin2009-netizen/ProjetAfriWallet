namespace AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;

public enum BankProviderCapability
{
    SepaCreditTransfer,
    SepaInstant,
    Swift,
    LocalTransfer,
    TransferStatus,
    Webhooks,
    Reconciliation
}
