namespace AfriWallet.Fraud.Investigation.Domain.Responses;

public enum FraudResponseType { NoAction = 0, Monitor = 1, ChallengeCustomer = 2, DeclineRecommended = 3, AccountRestrictionRecommended = 4, DeviceRevocationRecommended = 5 }