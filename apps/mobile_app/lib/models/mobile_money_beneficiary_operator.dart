enum MobileMoneyBeneficiaryOperator {
  mtnCameroon(
    backendValue: 'MTN',
    operatorCode: 'MTN_CM',
    displayName: 'MTN Mobile Money',
  ),
  orangeCameroon(
    backendValue: 'ORANGE',
    operatorCode: 'ORANGE_CM',
    displayName: 'Orange Money',
  );

  const MobileMoneyBeneficiaryOperator({
    required this.backendValue,
    required this.operatorCode,
    required this.displayName,
  });

  final String backendValue;
  final String operatorCode;
  final String displayName;
}

class MobileMoneyBeneficiaryOperatorMapper {
  const MobileMoneyBeneficiaryOperatorMapper._();

  static MobileMoneyBeneficiaryOperator? fromBackend({
    required String countryCode,
    required String? operator,
  }) {
    if (operator == null) {
      return null;
    }

    if (countryCode.trim().toUpperCase() != 'CM') {
      return null;
    }

    final normalizedOperator = operator.trim().toUpperCase();
    for (final candidate in MobileMoneyBeneficiaryOperator.values) {
      if (candidate.backendValue == normalizedOperator) {
        return candidate;
      }
    }

    return null;
  }
}
