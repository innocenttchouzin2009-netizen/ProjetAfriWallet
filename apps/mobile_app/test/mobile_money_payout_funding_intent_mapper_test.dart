import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/services/mobile_money_payout_funding_intent_mapper.dart';

void main() {
  group('MobileMoneyPayoutFundingIntentMapper', () {
    test('binds funding to the quote total debit including fees', () {
      final intent = MobileMoneyPayoutFundingIntentMapper.fromQuote(
        quote: _quote(
          sourceAmountMinor: 10000,
          totalFeeMinor: 500,
          totalSourceDebitMinor: 10500,
        ),
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 3000,
          ),
          _allocation(
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 7500,
          ),
        ],
        fundingSources: [
          _walletSource(availableMinor: 3000),
          _applePaySource(),
        ],
      );

      expect(intent.quoteId, 'quote-1');
      expect(intent.sourceCurrencyCode, 'EUR');
      expect(intent.totalSourceDebitMinor, 10500);
      expect(
        intent.fundingAllocations.fold<int>(
          0,
          (total, allocation) => total + allocation.amountMinor,
        ),
        10500,
      );
    });

    test('does not accept funding that covers source amount but omits fees', () {
      expect(
        () => MobileMoneyPayoutFundingIntentMapper.fromQuote(
          quote: _quote(
            sourceAmountMinor: 10000,
            totalFeeMinor: 500,
            totalSourceDebitMinor: 10500,
          ),
          fundingAllocations: [
            _allocation(
              sourceId: 'apple-pay-1',
              sourceType: FundingSourceType.applePay,
              amountMinor: 10000,
            ),
          ],
          fundingSources: [_applePaySource()],
        ),
        throwsArgumentError,
      );
    });

    test('rejects an internally inconsistent quote total', () {
      expect(
        () => MobileMoneyPayoutFundingIntentMapper.fromQuote(
          quote: _quote(
            sourceAmountMinor: 10000,
            totalFeeMinor: 500,
            totalSourceDebitMinor: 10499,
          ),
          fundingAllocations: [
            _allocation(
              sourceId: 'apple-pay-1',
              sourceType: FundingSourceType.applePay,
              amountMinor: 10499,
            ),
          ],
          fundingSources: [_applePaySource()],
        ),
        throwsArgumentError,
      );
    });

    test('rejects a selected source that is unavailable', () {
      expect(
        () => MobileMoneyPayoutFundingIntentMapper.fromQuote(
          quote: _quote(),
          fundingAllocations: [
            _allocation(
              sourceId: 'apple-pay-1',
              sourceType: FundingSourceType.applePay,
              amountMinor: 10500,
            ),
          ],
          fundingSources: [_applePaySource(isAvailable: false)],
        ),
        throwsArgumentError,
      );
    });
  });
}

MobileMoneyPayoutQuote _quote({
  String quoteId = 'quote-1',
  int sourceAmountMinor = 10000,
  int totalFeeMinor = 500,
  int totalSourceDebitMinor = 10500,
}) {
  return MobileMoneyPayoutQuote(
    quoteId: quoteId,
    sourceCurrencyCode: 'EUR',
    sourceAmountMinor: sourceAmountMinor,
    fees: const [
      MobileMoneyPayoutQuoteFee(
        code: 'TRANSFER_FEE',
        amountMinor: 500,
        currencyCode: 'EUR',
      ),
    ],
    totalFeeMinor: totalFeeMinor,
    totalSourceDebitMinor: totalSourceDebitMinor,
    destinationCurrencyCode: 'XAF',
    destinationAmountMinor: 655000,
    fxRate: 65.5,
    createdAtUtc: DateTime.utc(2026, 10, 9, 12),
    expiresAtUtc: DateTime.utc(2026, 10, 9, 12, 15),
  );
}

FundingAllocation _allocation({
  required String sourceId,
  required FundingSourceType sourceType,
  required int amountMinor,
}) {
  return FundingAllocation(
    sourceId: sourceId,
    sourceType: sourceType,
    amountMinor: amountMinor,
    currencyCode: 'EUR',
  );
}

FundingSource _walletSource({required int availableMinor}) {
  return FundingSource(
    id: 'wallet-1',
    type: FundingSourceType.wallet,
    displayLabel: 'Solde AfWal',
    currencyCode: 'EUR',
    isAvailable: true,
    availableMinor: availableMinor,
  );
}

FundingSource _applePaySource({bool isAvailable = true}) {
  return FundingSource(
    id: 'apple-pay-1',
    type: FundingSourceType.applePay,
    displayLabel: 'Apple Pay',
    currencyCode: 'EUR',
    isAvailable: isAvailable,
  );
}
