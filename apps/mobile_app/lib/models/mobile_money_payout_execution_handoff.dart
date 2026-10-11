import 'mobile_money_payout.dart';

class MobileMoneyPayoutExecutionHandoff {
  MobileMoneyPayoutExecutionHandoff({
    required this.quoteId,
    required this.sourceCurrencyCode,
    required this.totalSourceDebitMinor,
    required this.destinationCurrencyCode,
    required this.destinationAmountMinor,
    required Iterable<FundingAllocation> fundingAllocations,
    required this.preparedAtUtc,
    required this.quoteExpiresAtUtc,
  }) : fundingAllocations =
            List<FundingAllocation>.unmodifiable(fundingAllocations);

  final String quoteId;
  final String sourceCurrencyCode;
  final int totalSourceDebitMinor;
  final String destinationCurrencyCode;
  final int destinationAmountMinor;
  final List<FundingAllocation> fundingAllocations;
  final DateTime preparedAtUtc;
  final DateTime quoteExpiresAtUtc;

  void validate() {
    _requireNonEmpty(quoteId, 'quoteId');
    _validateCurrencyCode(sourceCurrencyCode, 'sourceCurrencyCode');
    _validateCurrencyCode(
      destinationCurrencyCode,
      'destinationCurrencyCode',
    );

    if (totalSourceDebitMinor <= 0) {
      throw ArgumentError.value(
        totalSourceDebitMinor,
        'totalSourceDebitMinor',
        'must be greater than zero',
      );
    }

    if (destinationAmountMinor <= 0) {
      throw ArgumentError.value(
        destinationAmountMinor,
        'destinationAmountMinor',
        'must be greater than zero',
      );
    }

    if (!quoteExpiresAtUtc.toUtc().isAfter(preparedAtUtc.toUtc())) {
      throw StateError(
        'Execution handoff requires a quote that is fresh at preparation time.',
      );
    }

    if (fundingAllocations.isEmpty) {
      throw ArgumentError.value(
        fundingAllocations,
        'fundingAllocations',
        'must not be empty',
      );
    }

    final seenSourceIds = <String>{};
    var allocatedMinor = 0;

    for (final allocation in fundingAllocations) {
      allocation.validate();

      if (allocation.currencyCode != sourceCurrencyCode) {
        throw ArgumentError.value(
          allocation.currencyCode,
          'fundingAllocations.currencyCode',
          'must match sourceCurrencyCode',
        );
      }

      if (!seenSourceIds.add(allocation.sourceId)) {
        throw ArgumentError.value(
          allocation.sourceId,
          'fundingAllocations.sourceId',
          'must be unique',
        );
      }

      allocatedMinor += allocation.amountMinor;
    }

    if (allocatedMinor != totalSourceDebitMinor) {
      throw ArgumentError.value(
        allocatedMinor,
        'fundingAllocations',
        'must sum to totalSourceDebitMinor',
      );
    }
  }
}

void _requireNonEmpty(String value, String name) {
  if (value.trim().isEmpty) {
    throw ArgumentError.value(value, name, 'must not be empty');
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
