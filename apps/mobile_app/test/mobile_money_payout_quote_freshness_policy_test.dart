import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/models/mobile_money_payout_quote_freshness.dart';
import 'package:mobile_app/services/mobile_money_payout_quote_freshness_policy.dart';

void main() {
  const policy = MobileMoneyPayoutQuoteFreshnessPolicy();

  test('classifies a quote as fresh before its expiry', () {
    final freshness = policy.evaluate(
      quote: _quote(),
      atUtc: DateTime.utc(2026, 10, 11, 12, 5),
    );

    expect(
      freshness.status,
      MobileMoneyPayoutQuoteFreshnessStatus.fresh,
    );
    expect(freshness.isFresh, isTrue);
    expect(freshness.isExpired, isFalse);
    expect(freshness.remaining, const Duration(minutes: 5));
    expect(freshness.evaluatedAtUtc.isUtc, isTrue);
    expect(freshness.expiresAtUtc, DateTime.utc(2026, 10, 11, 12, 10));
  });

  test('treats the exact expiry instant as expired', () {
    final freshness = policy.evaluate(
      quote: _quote(),
      atUtc: DateTime.utc(2026, 10, 11, 12, 10),
    );

    expect(
      freshness.status,
      MobileMoneyPayoutQuoteFreshnessStatus.expired,
    );
    expect(freshness.isFresh, isFalse);
    expect(freshness.isExpired, isTrue);
    expect(freshness.remaining, Duration.zero);
  });

  test('keeps remaining duration at zero after expiry', () {
    final freshness = policy.evaluate(
      quote: _quote(),
      atUtc: DateTime.utc(2026, 10, 11, 12, 15),
    );

    expect(freshness.isExpired, isTrue);
    expect(freshness.remaining, Duration.zero);
  });

  test('normalizes the evaluation instant to UTC', () {
    final freshness = policy.evaluate(
      quote: _quote(),
      atUtc: DateTime.parse('2026-10-11T14:05:00+02:00'),
    );

    expect(freshness.evaluatedAtUtc, DateTime.utc(2026, 10, 11, 12, 5));
    expect(freshness.remaining, const Duration(minutes: 5));
  });

  test('fails closed when quote timestamps violate the domain invariant', () {
    final quote = _quote(
      createdAtUtc: DateTime.utc(2026, 10, 11, 12, 10),
      expiresAtUtc: DateTime.utc(2026, 10, 11, 12, 10),
    );

    expect(
      () => policy.evaluate(
        quote: quote,
        atUtc: DateTime.utc(2026, 10, 11, 12, 5),
      ),
      throwsStateError,
    );
  });
}

MobileMoneyPayoutQuote _quote({
  DateTime? createdAtUtc,
  DateTime? expiresAtUtc,
}) {
  return MobileMoneyPayoutQuote(
    quoteId: 'quote-1',
    sourceCurrencyCode: 'EUR',
    sourceAmountMinor: 10000,
    fees: const <MobileMoneyPayoutQuoteFee>[],
    totalFeeMinor: 0,
    totalSourceDebitMinor: 10000,
    destinationCurrencyCode: 'XAF',
    destinationAmountMinor: 6500000,
    fxRate: 650,
    createdAtUtc: createdAtUtc ?? DateTime.utc(2026, 10, 11, 12),
    expiresAtUtc:
        expiresAtUtc ?? DateTime.utc(2026, 10, 11, 12, 10),
  );
}
