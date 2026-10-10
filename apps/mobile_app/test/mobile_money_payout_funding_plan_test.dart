import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_plan.dart';

void main() {
  group('MobileMoneyPayoutFundingPlan', () {
    test('preserves the authoritative backend funding plan', () {
      final plan = _plan();

      expect(plan.correlationId, '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4');
      expect(plan.requiredAmountMinor, 10250);
      expect(plan.currencyCode, 'EUR');
      expect(plan.allocations, hasLength(2));
      expect(plan.allocations.first.amountMinor, 2500);
      expect(plan.allocations.last.amountMinor, 7750);
      expect(plan.plannedAtUtc.isUtc, isTrue);
    });

    test('exposes allocations as an immutable domain snapshot', () {
      final plan = _plan();

      expect(
        () => plan.allocations.add(
          const FundingAllocation(
            sourceId: 'sepa-1',
            sourceType: FundingSourceType.sepa,
            amountMinor: 1,
            currencyCode: 'EUR',
          ),
        ),
        throwsUnsupportedError,
      );
    });
  });
}

MobileMoneyPayoutFundingPlan _plan() {
  return MobileMoneyPayoutFundingPlan(
    correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
    requiredAmountMinor: 10250,
    currencyCode: 'EUR',
    allocations: const <FundingAllocation>[
      FundingAllocation(
        sourceId: 'wallet-1',
        sourceType: FundingSourceType.wallet,
        amountMinor: 2500,
        currencyCode: 'EUR',
      ),
      FundingAllocation(
        sourceId: 'google-pay-1',
        sourceType: FundingSourceType.googlePay,
        amountMinor: 7750,
        currencyCode: 'EUR',
      ),
    ],
    plannedAtUtc: DateTime.utc(2026, 10, 10, 10, 30, 1),
  );
}
