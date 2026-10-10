class MobileMoneyPayoutFundingAllocationDto {
  const MobileMoneyPayoutFundingAllocationDto({
    required this.sourceId,
    required this.sourceType,
    required this.amountMinor,
    required this.currencyCode,
  });

  final String sourceId;
  final int sourceType;
  final int amountMinor;
  final String currencyCode;
}

class MobileMoneyPayoutFundingRequestDto {
  MobileMoneyPayoutFundingRequestDto({
    required this.correlationId,
    required this.requiredAmountMinor,
    required this.currencyCode,
    required Iterable<MobileMoneyPayoutFundingAllocationDto> allocations,
    required this.requestedAtUtc,
  }) : allocations = List<MobileMoneyPayoutFundingAllocationDto>.unmodifiable(
         allocations,
       );

  final String correlationId;
  final int requiredAmountMinor;
  final String currencyCode;
  final List<MobileMoneyPayoutFundingAllocationDto> allocations;
  final DateTime requestedAtUtc;
}

class MobileMoneyPayoutFundingPlanResponseDto {
  MobileMoneyPayoutFundingPlanResponseDto({
    required this.correlationId,
    required this.requiredAmountMinor,
    required this.currencyCode,
    required Iterable<MobileMoneyPayoutFundingAllocationDto> allocations,
    required this.plannedAtUtc,
  }) : allocations = List<MobileMoneyPayoutFundingAllocationDto>.unmodifiable(
         allocations,
       );

  final String correlationId;
  final int requiredAmountMinor;
  final String currencyCode;
  final List<MobileMoneyPayoutFundingAllocationDto> allocations;
  final DateTime plannedAtUtc;
}
