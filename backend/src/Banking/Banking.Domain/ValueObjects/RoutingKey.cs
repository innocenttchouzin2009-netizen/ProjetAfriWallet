namespace AfriWallet.Banking.Domain.ValueObjects;

public sealed record RoutingKey(string Country, string Currency, string Scheme, string Environment);
