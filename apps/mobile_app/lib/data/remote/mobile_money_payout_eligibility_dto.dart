class MobileMoneyPayoutEligibilityRequestDto {
  const MobileMoneyPayoutEligibilityRequestDto({
    required this.sourceCountryCode,
    required this.sourceCurrency,
    required this.destinationCountryCode,
    required this.destinationCurrency,
    required this.operatorCode,
  });

  final String sourceCountryCode;
  final String sourceCurrency;
  final String destinationCountryCode;
  final String destinationCurrency;
  final String operatorCode;
}

class MobileMoneyPayoutEligibilityResponseDto {
  const MobileMoneyPayoutEligibilityResponseDto({
    required this.isEligible,
    required this.failureCode,
  });

  final bool isEligible;
  final String? failureCode;
}
