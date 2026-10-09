import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/services/mobile_money_payout_funding_allocation_planner.dart';

void main() {
  group('MobileMoneyPayoutFundingAllocationPlanner', () {
    test('plans external-only funding for the exact quoted total debit', () {
      final allocations =
          MobileMoneyPayoutFundingAllocationPlanner.planExternalOnly(
        quote: _quote(),
        externalSource: _applePaySource(),
      );

      expect(allocations, hasLength(1));
      expect(allocations.single.sourceId, 'apple-pay-1');
      expect(allocations.single.sourceType, FundingSourceType.applePay);
      expect(allocations.single.amountMinor, 10500);
      expect(allocations.single.currencyCode, 'EUR');
    });

    test('plans wallet-only funding only when the wallet can cover the total',
        () {
      final allocations =
          MobileMoneyPayoutFundingAllocationPlanner.planWalletOnly(
        quote: _quote(),
        walletSource: _walletSource(availableMinor: 12000),
      );

      expect(allocations, hasLength(1));
      expect(allocations.single.sourceId, 'wallet-1');
      expect(allocations.single.sourceType, FundingSourceType.wallet);
      expect(allocations.single.amountMinor, 10500);
    });

    test('plans an explicit wallet plus external split', () {
      final allocations = MobileMoneyPayoutFundingAllocationPlanner.planSplit(
        quote: _quote(),
        walletSource: _walletSource(availableMinor: 3000),
        externalSource: _cardSource(),
        walletAmountMinor: 3000,
      );

      expect(allocations, hasLength(2));
      expect(allocations.first.sourceType, FundingSourceType.wallet);
      expect(allocations.first.amountMinor, 3000);
      expect(allocations.last.sourceType, FundingSourceType.paymentCard);
      expect(allocations.last.amountMinor, 7500);
      expect(
        allocations.fold<int>(
          0,
          (total, allocation) => total + allocation.amountMinor,
        ),
        10500,
      );
    });

    test('does not allow wallet-only funding above the available balance', () {
      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planWalletOnly(
          quote: _quote(),
          walletSource: _walletSource(availableMinor: 10499),
        ),
        throwsArgumentError,
      );
    });

    test('requires split wallet amount to be explicitly partial', () {
      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planSplit(
          quote: _quote(),
          walletSource: _walletSource(availableMinor: 10500),
          externalSource: _applePaySource(),
          walletAmountMinor: 0,
        ),
        throwsArgumentError,
      );

      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planSplit(
          quote: _quote(),
          walletSource: _walletSource(availableMinor: 10500),
          externalSource: _applePaySource(),
          walletAmountMinor: 10500,
        ),
        throwsArgumentError,
      );
    });

    test('does not treat a wallet source as an external payment method', () {
      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planExternalOnly(
          quote: _quote(),
          externalSource: _walletSource(availableMinor: 20000),
        ),
        throwsArgumentError,
      );
    });

    test('rejects unavailable or wrong-currency selected sources', () {
      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planExternalOnly(
          quote: _quote(),
          externalSource: _applePaySource(isAvailable: false),
        ),
        throwsArgumentError,
      );

      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planExternalOnly(
          quote: _quote(),
          externalSource: _applePaySource(currencyCode: 'USD'),
        ),
        throwsArgumentError,
      );
    });

    test('rejects a split that exceeds the explicit wallet balance', () {
      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planSplit(
          quote: _quote(),
          walletSource: _walletSource(availableMinor: 2999),
          externalSource: _applePaySource(),
          walletAmountMinor: 3000,
        ),
        throwsArgumentError,
      );
    });

    test('rejects an inconsistent quote total before planning funding', () {
      expect(
        () => MobileMoneyPayoutFundingAllocationPlanner.planExternalOnly(
          quote: _quote(totalSourceDebitMinor: 10499),
          externalSource: _applePaySource(),
        ),
        throwsArgumentError,
      );
    });

    test('returns an immutable allocation plan', () {
      final allocations =
          MobileMoneyPayoutFundingAllocationPlanner.planExternalOnly(
        quote: _quote(),
        externalSource: _applePaySource(),
      );

      expect(
        () => allocations.add(
          const FundingAllocation(
            sourceId: 'card-1',
            sourceType: FundingSourceType.paymentCard,
            amountMinor: 1,
            currencyCode: 'EUR',
          ),
        ),
        throwsUnsupportedError,
      );
    });
  });
}

MobileMoneyPayoutQuote _quote({
  int sourceAmountMinor = 10000,
  int totalFeeMinor = 500,
  int totalSourceDebitMinor = 10500,
}) {
  return MobileMoneyPayoutQuote(
    quoteId: 'quote-1',
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

FundingSource _walletSource({
  required int availableMinor,
  bool isAvailable = true,
  String currencyCode = 'EUR',
}) {
  return FundingSource(
    id: 'wallet-1',
    type: FundingSourceType.wallet,
    displayLabel: 'Solde AfWal',
    currencyCode: currencyCode,
    isAvailable: isAvailable,
    availableMinor: availableMinor,
  );
}

FundingSource _applePaySource({
  bool isAvailable = true,
  String currencyCode = 'EUR',
}) {
  return FundingSource(
    id: 'apple-pay-1',
    type: FundingSourceType.applePay,
    displayLabel: 'Apple Pay',
    currencyCode: currencyCode,
    isAvailable: isAvailable,
  );
}

FundingSource _cardSource({
  bool isAvailable = true,
  String currencyCode = 'EUR',
}) {
  return FundingSource(
    id: 'card-1',
    type: FundingSourceType.paymentCard,
    displayLabel: 'Carte bancaire',
    currencyCode: currencyCode,
    isAvailable: isAvailable,
  );
}
