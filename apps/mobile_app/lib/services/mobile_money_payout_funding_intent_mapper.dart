import '../models/mobile_money_payout.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_quote.dart';

class MobileMoneyPayoutFundingIntentMapper {
  const MobileMoneyPayoutFundingIntentMapper._();

  static MobileMoneyPayoutFundingIntent fromQuote({
    required MobileMoneyPayoutQuote quote,
    required Iterable<FundingAllocation> fundingAllocations,
    required Iterable<FundingSource> fundingSources,
  }) {
    _validateQuoteFundingBasis(quote);

    final intent = MobileMoneyPayoutFundingIntent(
      quoteId: quote.quoteId,
      sourceCurrencyCode: quote.sourceCurrencyCode,
      totalSourceDebitMinor: quote.totalSourceDebitMinor,
      fundingAllocations: fundingAllocations,
    );

    intent.validate(fundingSources: fundingSources);
    return intent;
  }

  static void _validateQuoteFundingBasis(MobileMoneyPayoutQuote quote) {
    if (quote.quoteId.trim().isEmpty) {
      throw ArgumentError.value(quote.quoteId, 'quote.quoteId', 'must not be empty');
    }

    if (!RegExp(r'^[A-Z]{3}$').hasMatch(quote.sourceCurrencyCode)) {
      throw ArgumentError.value(
        quote.sourceCurrencyCode,
        'quote.sourceCurrencyCode',
        'must be an uppercase ISO-4217 code',
      );
    }

    if (quote.sourceAmountMinor <= 0) {
      throw ArgumentError.value(
        quote.sourceAmountMinor,
        'quote.sourceAmountMinor',
        'must be greater than zero',
      );
    }

    if (quote.totalFeeMinor < 0) {
      throw ArgumentError.value(
        quote.totalFeeMinor,
        'quote.totalFeeMinor',
        'must not be negative',
      );
    }

    final expectedTotalSourceDebitMinor =
        quote.sourceAmountMinor + quote.totalFeeMinor;
    if (quote.totalSourceDebitMinor != expectedTotalSourceDebitMinor) {
      throw ArgumentError.value(
        quote.totalSourceDebitMinor,
        'quote.totalSourceDebitMinor',
        'must equal sourceAmountMinor plus totalFeeMinor',
      );
    }
  }
}
