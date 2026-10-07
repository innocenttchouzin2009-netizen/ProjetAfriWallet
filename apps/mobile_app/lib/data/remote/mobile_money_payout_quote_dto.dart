class MobileMoneyPayoutQuoteRequestDto {
  const MobileMoneyPayoutQuoteRequestDto({
    required this.sourceCountryCode,
    required this.sourceCurrency,
    required this.destinationCountryCode,
    required this.destinationCurrency,
    required this.operatorCode,
    required this.sourceAmountMinor,
  });

  final String sourceCountryCode;
  final String sourceCurrency;
  final String destinationCountryCode;
  final String destinationCurrency;
  final String operatorCode;
  final int sourceAmountMinor;
}

class MobileMoneyPayoutQuoteFeeDto {
  const MobileMoneyPayoutQuoteFeeDto({
    required this.code,
    required this.amountMinor,
    required this.currency,
  });

  final String code;
  final int amountMinor;
  final String currency;
}

class MobileMoneyPayoutQuoteResponseDto {
  const MobileMoneyPayoutQuoteResponseDto({
    required this.quoteId,
    required this.sourceCurrency,
    required this.sourceAmountMinor,
    required this.fees,
    required this.totalFeeMinor,
    required this.totalSourceDebitMinor,
    required this.destinationCurrency,
    required this.destinationAmountMinor,
    required this.fxRate,
    required this.createdAtUtc,
    required this.expiresAtUtc,
  });

  final String quoteId;
  final String sourceCurrency;
  final int sourceAmountMinor;
  final List<MobileMoneyPayoutQuoteFeeDto> fees;
  final int totalFeeMinor;
  final int totalSourceDebitMinor;
  final String destinationCurrency;
  final int destinationAmountMinor;
  final double fxRate;
  final DateTime createdAtUtc;
  final DateTime expiresAtUtc;
}
