namespace AfriWallet.Merchants.Settlement.Domain.Settlements;
public enum MerchantSettlementStatus { Created=0, Validated=1, DispatchPending=2, Dispatched=3, Acknowledged=4, RetryPending=5, CompensationRequired=6, Compensated=7, ManualInterventionRequired=8, Failed=9, Completed=10 }
