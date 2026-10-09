import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';

void main() {
  group('MobileMoneyPayoutFundingIntent', () {
    test('accepts wallet-only funding for the exact quoted total debit', () {
      final intent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 10500,
          ),
        ],
      );

      expect(
        () => intent.validate(
          fundingSources: [_walletSource(availableMinor: 20000)],
        ),
        returnsNormally,
      );
    });

    test('accepts external-only funding for the exact quoted total debit', () {
      final intent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 10500,
          ),
        ],
      );

      expect(
        () => intent.validate(fundingSources: [_applePaySource()]),
        returnsNormally,
      );
    });

    test('accepts one wallet source plus one external source', () {
      final intent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 3000,
          ),
          _allocation(
            sourceId: 'card-1',
            sourceType: FundingSourceType.paymentCard,
            amountMinor: 7500,
          ),
        ],
      );

      expect(
        () => intent.validate(
          fundingSources: [
            _walletSource(availableMinor: 3000),
            _cardSource(),
          ],
        ),
        returnsNormally,
      );
    });

    test('rejects split funding across multiple external sources', () {
      final intent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 5000,
          ),
          _allocation(
            sourceId: 'card-1',
            sourceType: FundingSourceType.paymentCard,
            amountMinor: 5500,
          ),
        ],
      );

      expect(intent.validate, throwsArgumentError);
    });

    test('rejects funding across multiple wallet sources', () {
      final intent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 5000,
          ),
          _allocation(
            sourceId: 'wallet-2',
            sourceType: FundingSourceType.wallet,
            amountMinor: 5500,
          ),
        ],
      );

      expect(intent.validate, throwsArgumentError);
    });

    test('requires allocations to cover the exact total debit including fees', () {
      final intent = _intent(
        totalSourceDebitMinor: 10500,
        fundingAllocations: [
          _allocation(
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 10000,
          ),
        ],
      );

      expect(intent.validate, throwsArgumentError);
    });

    test('requires allocation currency to match the funding currency', () {
      final intent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 10500,
            currencyCode: 'USD',
          ),
        ],
      );

      expect(intent.validate, throwsArgumentError);
    });

    test('rejects duplicate funding source ids', () {
      final intent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'source-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 3000,
          ),
          _allocation(
            sourceId: 'source-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 7500,
          ),
        ],
      );

      expect(intent.validate, throwsArgumentError);
    });

    test('rejects unavailable sources and wallet over-allocation', () {
      final unavailableIntent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 10500,
          ),
        ],
      );

      expect(
        () => unavailableIntent.validate(
          fundingSources: [_applePaySource(isAvailable: false)],
        ),
        throwsArgumentError,
      );

      final walletIntent = _intent(
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 10500,
          ),
        ],
      );

      expect(
        () => walletIntent.validate(
          fundingSources: [_walletSource(availableMinor: 10499)],
        ),
        throwsArgumentError,
      );
    });

    test('snapshots allocations so later list mutations cannot change intent', () {
      final allocations = [
        _allocation(
          sourceId: 'apple-pay-1',
          sourceType: FundingSourceType.applePay,
          amountMinor: 10500,
        ),
      ];
      final intent = _intent(fundingAllocations: allocations);

      allocations.clear();

      expect(intent.fundingAllocations, hasLength(1));
      expect(intent.fundingAllocations.first.sourceId, 'apple-pay-1');
    });
  });
}

MobileMoneyPayoutFundingIntent _intent({
  String quoteId = 'quote-1',
  String sourceCurrencyCode = 'EUR',
  int totalSourceDebitMinor = 10500,
  List<FundingAllocation>? fundingAllocations,
}) {
  return MobileMoneyPayoutFundingIntent(
    quoteId: quoteId,
    sourceCurrencyCode: sourceCurrencyCode,
    totalSourceDebitMinor: totalSourceDebitMinor,
    fundingAllocations: fundingAllocations ??
        [
          _allocation(
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: totalSourceDebitMinor,
            currencyCode: sourceCurrencyCode,
          ),
        ],
  );
}

FundingAllocation _allocation({
  required String sourceId,
  required FundingSourceType sourceType,
  required int amountMinor,
  String currencyCode = 'EUR',
}) {
  return FundingAllocation(
    sourceId: sourceId,
    sourceType: sourceType,
    amountMinor: amountMinor,
    currencyCode: currencyCode,
  );
}

FundingSource _walletSource({
  int availableMinor = 10500,
  bool isAvailable = true,
}) {
  return FundingSource(
    id: 'wallet-1',
    type: FundingSourceType.wallet,
    displayLabel: 'Solde AfWal',
    currencyCode: 'EUR',
    isAvailable: isAvailable,
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

FundingSource _cardSource({bool isAvailable = true}) {
  return FundingSource(
    id: 'card-1',
    type: FundingSourceType.paymentCard,
    displayLabel: 'Carte bancaire',
    currencyCode: 'EUR',
    isAvailable: isAvailable,
  );
}
