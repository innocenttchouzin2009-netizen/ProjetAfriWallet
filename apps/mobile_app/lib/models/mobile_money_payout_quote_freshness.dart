enum MobileMoneyPayoutQuoteFreshnessStatus {
  fresh,
  expired,
}

class MobileMoneyPayoutQuoteFreshness {
  const MobileMoneyPayoutQuoteFreshness({
    required this.status,
    required this.evaluatedAtUtc,
    required this.expiresAtUtc,
    required this.remaining,
  });

  final MobileMoneyPayoutQuoteFreshnessStatus status;
  final DateTime evaluatedAtUtc;
  final DateTime expiresAtUtc;
  final Duration remaining;

  bool get isFresh =>
      status == MobileMoneyPayoutQuoteFreshnessStatus.fresh;

  bool get isExpired =>
      status == MobileMoneyPayoutQuoteFreshnessStatus.expired;
}
