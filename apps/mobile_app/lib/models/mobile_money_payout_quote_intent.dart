class BeneficiaryQuoteDraft {
  const BeneficiaryQuoteDraft({
    required this.normalizedPhoneNumber,
    required this.countryCode,
    required this.operatorCode,
    required this.accountHolderName,
  });

  final String normalizedPhoneNumber;
  final String countryCode;
  final String operatorCode;
  final String accountHolderName;

  void validate() {
    if (!RegExp(r'^\+[1-9][0-9]{1,14}$').hasMatch(normalizedPhoneNumber)) {
      throw ArgumentError.value(
        normalizedPhoneNumber,
        'normalizedPhoneNumber',
        'must be a valid E.164 phone number',
      );
    }

    if (!RegExp(r'^[A-Z]{2}$').hasMatch(countryCode)) {
      throw ArgumentError.value(
        countryCode,
        'countryCode',
        'must be an uppercase ISO-3166 alpha-2 code',
      );
    }

    _requireNonEmpty(operatorCode, 'operatorCode');
    _requireNonEmpty(accountHolderName, 'accountHolderName');
  }
}

class MobileMoneyPayoutQuoteIntent {
  const MobileMoneyPayoutQuoteIntent({
    required this.sourceCountryCode,
    required this.sourceCurrencyCode,
    required this.destinationCurrencyCode,
    required this.sourceAmountMinor,
    required this.beneficiary,
  });

  final String sourceCountryCode;
  final String sourceCurrencyCode;
  final String destinationCurrencyCode;
  final int sourceAmountMinor;
  final BeneficiaryQuoteDraft beneficiary;

  void validate() {
    _validateCountryCode(sourceCountryCode, 'sourceCountryCode');
    _validateCurrencyCode(sourceCurrencyCode, 'sourceCurrencyCode');
    _validateCurrencyCode(
      destinationCurrencyCode,
      'destinationCurrencyCode',
    );

    if (sourceAmountMinor <= 0) {
      throw ArgumentError.value(
        sourceAmountMinor,
        'sourceAmountMinor',
        'must be greater than zero',
      );
    }

    beneficiary.validate();
  }
}

void _requireNonEmpty(String value, String name) {
  if (value.trim().isEmpty) {
    throw ArgumentError.value(value, name, 'must not be empty');
  }
}

void _validateCountryCode(String value, String name) {
  if (!RegExp(r'^[A-Z]{2}$').hasMatch(value)) {
    throw ArgumentError.value(
      value,
      name,
      'must be an uppercase ISO-3166 alpha-2 code',
    );
  }
}

void _validateCurrencyCode(String value, String name) {
  if (!RegExp(r'^[A-Z]{3}$').hasMatch(value)) {
    throw ArgumentError.value(
      value,
      name,
      'must be an uppercase ISO-4217 code',
    );
  }
}
