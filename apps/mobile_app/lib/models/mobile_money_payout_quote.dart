class MobileMoneyPayoutQuoteFee {
  const MobileMoneyPayoutQuoteFee({
    required this.code,
    required this.amountMinor,
    required this.currencyCode,
  });

  final String code;
  final int amountMinor;
  final String currencyCode;
}

class MobileMoneyPayoutQuote {
  const MobileMoneyPayoutQuote({
    required this.quoteId,
    required this.sourceCurrencyCode,
    required this.sourceAmountMinor,
    required this.fees,
    required this.totalFeeMinor,
    required this.totalSourceDebitMinor,
    required this.destinationCurrencyCode,
    required this.destinationAmountMinor,
    required this.fxRate,
    required this.createdAtUtc,
    required this.expiresAtUtc,
  });

  final String quoteId;
  final String sourceCurrencyCode;
  final int sourceAmountMinor;
  final List<MobileMoneyPayoutQuoteFee> fees;
  final int totalFeeMinor;
  final int totalSourceDebitMinor;
  final String destinationCurrencyCode;
  final int destinationAmountMinor;

  /// Informational server-provided FX rate.
  ///
  /// Monetary amounts must remain authoritative from the backend minor-unit
  /// fields and must not be recomputed from this value on the client.
  final double fxRate;

  final DateTime createdAtUtc;
  final DateTime expiresAtUtc;
}
