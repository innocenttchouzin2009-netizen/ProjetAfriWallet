import '../models/mobile_money_payout_execution_handoff.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_quote.dart';
import 'mobile_money_payout_quote_freshness_policy.dart';

class MobileMoneyPayoutExecutionHandoffPolicy {
  const MobileMoneyPayoutExecutionHandoffPolicy();

  MobileMoneyPayoutExecutionHandoff prepare({
    required MobileMoneyPayoutQuote quote,
    required MobileMoneyPayoutFundingIntent fundingIntent,
    required DateTime atUtc,
  }) {
    fundingIntent.validate();

    final freshness = const MobileMoneyPayoutQuoteFreshnessPolicy().evaluate(
      quote: quote,
      atUtc: atUtc,
    );

    if (freshness.isExpired) {
      throw StateError(
        'Payout quote must be fresh before execution handoff.',
      );
    }

    _validateQuote(quote);

    if (fundingIntent.quoteId != quote.quoteId) {
      throw ArgumentError.value(
        fundingIntent.quoteId,
        'fundingIntent.quoteId',
        'must match quote.quoteId',
      );
    }

    if (fundingIntent.sourceCurrencyCode != quote.sourceCurrencyCode) {
      throw ArgumentError.value(
        fundingIntent.sourceCurrencyCode,
        'fundingIntent.sourceCurrencyCode',
        'must match quote.sourceCurrencyCode',
      );
    }

    if (fundingIntent.totalSourceDebitMinor != quote.totalSourceDebitMinor) {
      throw ArgumentError.value(
        fundingIntent.totalSourceDebitMinor,
        'fundingIntent.totalSourceDebitMinor',
        'must match quote.totalSourceDebitMinor',
      );
    }

    final handoff = MobileMoneyPayoutExecutionHandoff(
      quoteId: quote.quoteId,
      sourceCurrencyCode: quote.sourceCurrencyCode,
      totalSourceDebitMinor: quote.totalSourceDebitMinor,
      destinationCurrencyCode: quote.destinationCurrencyCode,
      destinationAmountMinor: quote.destinationAmountMinor,
      fundingAllocations: fundingIntent.fundingAllocations,
      preparedAtUtc: freshness.evaluatedAtUtc,
      quoteExpiresAtUtc: freshness.expiresAtUtc,
    );

    handoff.validate();
    return handoff;
  }

  void _validateQuote(MobileMoneyPayoutQuote quote) {
    if (quote.quoteId.trim().isEmpty) {
      throw ArgumentError.value(
        quote.quoteId,
        'quote.quoteId',
        'must not be empty',
      );
    }

    _validateCurrencyCode(
      quote.sourceCurrencyCode,
      'quote.sourceCurrencyCode',
    );
    _validateCurrencyCode(
      quote.destinationCurrencyCode,
      'quote.destinationCurrencyCode',
    );

    if (quote.totalSourceDebitMinor <= 0) {
      throw ArgumentError.value(
        quote.totalSourceDebitMinor,
        'quote.totalSourceDebitMinor',
        'must be greater than zero',
      );
    }

    if (quote.destinationAmountMinor <= 0) {
      throw ArgumentError.value(
        quote.destinationAmountMinor,
        'quote.destinationAmountMinor',
        'must be greater than zero',
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
}
