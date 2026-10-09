import '../models/mobile_money_payout.dart';
import '../models/mobile_money_payout_quote.dart';

class MobileMoneyPayoutFundingAllocationPlanner {
  const MobileMoneyPayoutFundingAllocationPlanner._();

  static List<FundingAllocation> planWalletOnly({
    required MobileMoneyPayoutQuote quote,
    required FundingSource walletSource,
  }) {
    _validateQuoteFundingBasis(quote);
    _validateWalletSource(walletSource, quote);

    if (walletSource.availableMinor! < quote.totalSourceDebitMinor) {
      throw ArgumentError.value(
        walletSource.availableMinor,
        'walletSource.availableMinor',
        'must cover totalSourceDebitMinor for wallet-only funding',
      );
    }

    return List<FundingAllocation>.unmodifiable([
      FundingAllocation(
        sourceId: walletSource.id,
        sourceType: FundingSourceType.wallet,
        amountMinor: quote.totalSourceDebitMinor,
        currencyCode: quote.sourceCurrencyCode,
      ),
    ]);
  }

  static List<FundingAllocation> planExternalOnly({
    required MobileMoneyPayoutQuote quote,
    required FundingSource externalSource,
  }) {
    _validateQuoteFundingBasis(quote);
    _validateExternalSource(externalSource, quote);

    return List<FundingAllocation>.unmodifiable([
      FundingAllocation(
        sourceId: externalSource.id,
        sourceType: externalSource.type,
        amountMinor: quote.totalSourceDebitMinor,
        currencyCode: quote.sourceCurrencyCode,
      ),
    ]);
  }

  static List<FundingAllocation> planSplit({
    required MobileMoneyPayoutQuote quote,
    required FundingSource walletSource,
    required FundingSource externalSource,
    required int walletAmountMinor,
  }) {
    _validateQuoteFundingBasis(quote);
    _validateWalletSource(walletSource, quote);
    _validateExternalSource(externalSource, quote);

    if (walletSource.id == externalSource.id) {
      throw ArgumentError.value(
        externalSource.id,
        'externalSource.id',
        'must differ from walletSource.id',
      );
    }

    if (walletAmountMinor <= 0) {
      throw ArgumentError.value(
        walletAmountMinor,
        'walletAmountMinor',
        'must be greater than zero',
      );
    }

    if (walletAmountMinor >= quote.totalSourceDebitMinor) {
      throw ArgumentError.value(
        walletAmountMinor,
        'walletAmountMinor',
        'must be less than totalSourceDebitMinor for split funding',
      );
    }

    if (walletAmountMinor > walletSource.availableMinor!) {
      throw ArgumentError.value(
        walletAmountMinor,
        'walletAmountMinor',
        'must not exceed wallet availableMinor',
      );
    }

    final externalAmountMinor =
        quote.totalSourceDebitMinor - walletAmountMinor;

    return List<FundingAllocation>.unmodifiable([
      FundingAllocation(
        sourceId: walletSource.id,
        sourceType: FundingSourceType.wallet,
        amountMinor: walletAmountMinor,
        currencyCode: quote.sourceCurrencyCode,
      ),
      FundingAllocation(
        sourceId: externalSource.id,
        sourceType: externalSource.type,
        amountMinor: externalAmountMinor,
        currencyCode: quote.sourceCurrencyCode,
      ),
    ]);
  }

  static void _validateWalletSource(
    FundingSource source,
    MobileMoneyPayoutQuote quote,
  ) {
    _validateSelectedSource(source, quote);

    if (source.type != FundingSourceType.wallet) {
      throw ArgumentError.value(
        source.type,
        'walletSource.type',
        'must be wallet',
      );
    }
  }

  static void _validateExternalSource(
    FundingSource source,
    MobileMoneyPayoutQuote quote,
  ) {
    _validateSelectedSource(source, quote);

    if (source.type == FundingSourceType.wallet) {
      throw ArgumentError.value(
        source.type,
        'externalSource.type',
        'must not be wallet',
      );
    }
  }

  static void _validateSelectedSource(
    FundingSource source,
    MobileMoneyPayoutQuote quote,
  ) {
    source.validate();

    if (!source.isAvailable) {
      throw ArgumentError.value(
        source.id,
        'fundingSource.id',
        'must identify an available funding source',
      );
    }

    if (source.currencyCode != quote.sourceCurrencyCode) {
      throw ArgumentError.value(
        source.currencyCode,
        'fundingSource.currencyCode',
        'must match quote.sourceCurrencyCode',
      );
    }
  }

  static void _validateQuoteFundingBasis(MobileMoneyPayoutQuote quote) {
    if (quote.quoteId.trim().isEmpty) {
      throw ArgumentError.value(
        quote.quoteId,
        'quote.quoteId',
        'must not be empty',
      );
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
