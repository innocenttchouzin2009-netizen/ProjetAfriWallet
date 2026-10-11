import '../models/mobile_money_payout_quote.dart';
import '../models/mobile_money_payout_quote_freshness.dart';

class MobileMoneyPayoutQuoteFreshnessPolicy {
  const MobileMoneyPayoutQuoteFreshnessPolicy();

  MobileMoneyPayoutQuoteFreshness evaluate({
    required MobileMoneyPayoutQuote quote,
    required DateTime atUtc,
  }) {
    final createdAtUtc = quote.createdAtUtc.toUtc();
    final expiresAtUtc = quote.expiresAtUtc.toUtc();
    final evaluatedAtUtc = atUtc.toUtc();

    if (!expiresAtUtc.isAfter(createdAtUtc)) {
      throw StateError(
        'Payout quote expiry must be after its creation timestamp.',
      );
    }

    final isFresh = evaluatedAtUtc.isBefore(expiresAtUtc);

    return MobileMoneyPayoutQuoteFreshness(
      status: isFresh
          ? MobileMoneyPayoutQuoteFreshnessStatus.fresh
          : MobileMoneyPayoutQuoteFreshnessStatus.expired,
      evaluatedAtUtc: evaluatedAtUtc,
      expiresAtUtc: expiresAtUtc,
      remaining: isFresh
          ? expiresAtUtc.difference(evaluatedAtUtc)
          : Duration.zero,
    );
  }
}
