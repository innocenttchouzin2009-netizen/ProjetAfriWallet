import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/services/mobile_money_payout_execution_handoff_policy.dart';

void main() {
  const policy = MobileMoneyPayoutExecutionHandoffPolicy();

  test('prepares an immutable execution handoff for coherent fresh inputs', () {
    final handoff = policy.prepare(
      quote: _quote(),
      fundingIntent: _fundingIntent(),
      atUtc: DateTime.utc(2026, 10, 11, 12, 5),
    );

    expect(handoff.quoteId, 'quote-1');
    expect(handoff.sourceCurrencyCode, 'EUR');
    expect(handoff.totalSourceDebitMinor, 10500);
    expect(handoff.destinationCurrencyCode, 'XAF');
    expect(handoff.destinationAmountMinor, 6500000);
    expect(handoff.preparedAtUtc, DateTime.utc(2026, 10, 11, 12, 5));
    expect(handoff.quoteExpiresAtUtc, DateTime.utc(2026, 10, 11, 12, 10));
    expect(handoff.fundingAllocations, hasLength(1));
    expect(
      () => handoff.fundingAllocations.add(
        const FundingAllocation(
          sourceId: 'external-1',
          sourceType: FundingSourceType.paymentCard,
          amountMinor: 1,
          currencyCode: 'EUR',
        ),
      ),
      throwsUnsupportedError,
    );
  });

  test('normalizes handoff preparation time to UTC', () {
    final handoff = policy.prepare(
      quote: _quote(),
      fundingIntent: _fundingIntent(),
      atUtc: DateTime.parse('2026-10-11T14:05:00+02:00'),
    );

    expect(handoff.preparedAtUtc.isUtc, isTrue);
    expect(handoff.preparedAtUtc, DateTime.utc(2026, 10, 11, 12, 5));
  });

  test('fails closed at the exact quote expiry instant', () {
    expect(
      () => policy.prepare(
        quote: _quote(),
        fundingIntent: _fundingIntent(),
        atUtc: DateTime.utc(2026, 10, 11, 12, 10),
      ),
      throwsStateError,
    );
  });

  test('rejects a funding intent bound to another quote', () {
    expect(
      () => policy.prepare(
        quote: _quote(),
        fundingIntent: _fundingIntent(quoteId: 'quote-2'),
        atUtc: DateTime.utc(2026, 10, 11, 12, 5),
      ),
      throwsArgumentError,
    );
  });

  test('rejects a funding currency that differs from the quote', () {
    expect(
      () => policy.prepare(
        quote: _quote(),
        fundingIntent: _fundingIntent(
          sourceCurrencyCode: 'USD',
          fundingAllocations: const <FundingAllocation>[
            FundingAllocation(
              sourceId: 'wallet-1',
              sourceType: FundingSourceType.wallet,
              amountMinor: 10500,
              currencyCode: 'USD',
            ),
          ],
        ),
        atUtc: DateTime.utc(2026, 10, 11, 12, 5),
      ),
      throwsArgumentError,
    );
  });

  test('rejects a funding total that differs from quote debit total', () {
    expect(
      () => policy.prepare(
        quote: _quote(),
        fundingIntent: _fundingIntent(
          totalSourceDebitMinor: 10000,
          fundingAllocations: const <FundingAllocation>[
            FundingAllocation(
              sourceId: 'wallet-1',
              sourceType: FundingSourceType.wallet,
              amountMinor: 10000,
              currencyCode: 'EUR',
            ),
          ],
        ),
        atUtc: DateTime.utc(2026, 10, 11, 12, 5),
      ),
      throwsArgumentError,
    );
  });

  test('rejects malformed funding allocations before handoff', () {
    expect(
      () => policy.prepare(
        quote: _quote(),
        fundingIntent: _fundingIntent(
          fundingAllocations: const <FundingAllocation>[
            FundingAllocation(
              sourceId: 'wallet-1',
              sourceType: FundingSourceType.wallet,
              amountMinor: 10000,
              currencyCode: 'EUR',
            ),
          ],
        ),
        atUtc: DateTime.utc(2026, 10, 11, 12, 5),
      ),
      throwsArgumentError,
    );
  });

  test('rejects an invalid quote lifecycle before handoff', () {
    expect(
      () => policy.prepare(
        quote: _quote(
          createdAtUtc: DateTime.utc(2026, 10, 11, 12, 10),
          expiresAtUtc: DateTime.utc(2026, 10, 11, 12, 10),
        ),
        fundingIntent: _fundingIntent(),
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
    fees: const <MobileMoneyPayoutQuoteFee>[
      MobileMoneyPayoutQuoteFee(
        code: 'service',
        amountMinor: 500,
        currencyCode: 'EUR',
      ),
    ],
    totalFeeMinor: 500,
    totalSourceDebitMinor: 10500,
    destinationCurrencyCode: 'XAF',
    destinationAmountMinor: 6500000,
    fxRate: 650,
    createdAtUtc: createdAtUtc ?? DateTime.utc(2026, 10, 11, 12),
    expiresAtUtc:
        expiresAtUtc ?? DateTime.utc(2026, 10, 11, 12, 10),
  );
}

MobileMoneyPayoutFundingIntent _fundingIntent({
  String quoteId = 'quote-1',
  String sourceCurrencyCode = 'EUR',
  int totalSourceDebitMinor = 10500,
  List<FundingAllocation>? fundingAllocations,
}) {
  return MobileMoneyPayoutFundingIntent(
    quoteId: quoteId,
    sourceCurrencyCode: sourceCurrencyCode,
    totalSourceDebitMinor: totalSourceDebitMinor,
    fundingAllocations:
        fundingAllocations ??
        const <FundingAllocation>[
          FundingAllocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 10500,
            currencyCode: 'EUR',
          ),
        ],
  );
}
