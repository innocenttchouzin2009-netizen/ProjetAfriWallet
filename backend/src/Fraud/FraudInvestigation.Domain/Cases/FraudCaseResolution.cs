namespace AfriWallet.Fraud.Investigation.Domain.Cases;

public enum FraudCaseResolution { None = 0, FalsePositive = 1, ConfirmedFraud = 2, InsufficientEvidence = 3, CustomerVerificationRequired = 4 }