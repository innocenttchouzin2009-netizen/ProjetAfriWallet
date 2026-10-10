import 'mobile_money_payout.dart';

class MobileMoneyPayoutFundingPlan {
  MobileMoneyPayoutFundingPlan({
    required this.correlationId,
    required this.requiredAmountMinor,
    required this.currencyCode,
    required Iterable<FundingAllocation> allocations,
    required this.plannedAtUtc,
  }) : allocations = List<FundingAllocation>.unmodifiable(allocations);

  final String correlationId;
  final int requiredAmountMinor;
  final String currencyCode;
  final List<FundingAllocation> allocations;
  final DateTime plannedAtUtc;
}
