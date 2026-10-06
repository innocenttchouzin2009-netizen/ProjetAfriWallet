class MobileMoneyBeneficiaryLookupRequestDto {
  const MobileMoneyBeneficiaryLookupRequestDto({
    required this.phoneNumber,
  });

  final String phoneNumber;
}

class MobileMoneyBeneficiaryLookupResponseDto {
  const MobileMoneyBeneficiaryLookupResponseDto({
    required this.normalizedPhoneNumber,
    required this.countryCode,
    required this.operator,
    required this.operatorResolved,
    required this.accountHolderName,
    required this.beneficiaryResolved,
  });

  final String normalizedPhoneNumber;
  final String countryCode;
  final String? operator;
  final bool operatorResolved;
  final String? accountHolderName;
  final bool beneficiaryResolved;
}
