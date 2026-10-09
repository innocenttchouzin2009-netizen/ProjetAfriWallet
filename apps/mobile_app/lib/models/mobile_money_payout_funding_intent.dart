import 'mobile_money_payout.dart';

class MobileMoneyPayoutFundingIntent {
  MobileMoneyPayoutFundingIntent({
    required this.quoteId,
    required this.sourceCurrencyCode,
    required this.totalSourceDebitMinor,
    required Iterable<FundingAllocation> fundingAllocations,
  }) : fundingAllocations =
            List<FundingAllocation>.unmodifiable(fundingAllocations);

  final String quoteId;
  final String sourceCurrencyCode;
  final int totalSourceDebitMinor;
  final List<FundingAllocation> fundingAllocations;

  void validate({Iterable<FundingSource>? fundingSources}) {
    _requireNonEmpty(quoteId, 'quoteId');
    _validateCurrencyCode(sourceCurrencyCode, 'sourceCurrencyCode');

    if (totalSourceDebitMinor <= 0) {
      throw ArgumentError.value(
        totalSourceDebitMinor,
        'totalSourceDebitMinor',
        'must be greater than zero',
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
    var walletAllocationCount = 0;
    var externalAllocationCount = 0;

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

      if (allocation.sourceType == FundingSourceType.wallet) {
        walletAllocationCount += 1;
      } else {
        externalAllocationCount += 1;
      }

      allocatedMinor += allocation.amountMinor;
    }

    if (walletAllocationCount > 1) {
      throw ArgumentError.value(
        walletAllocationCount,
        'fundingAllocations',
        'must contain at most one wallet source',
      );
    }

    if (externalAllocationCount > 1) {
      throw ArgumentError.value(
        externalAllocationCount,
        'fundingAllocations',
        'must contain at most one external source',
      );
    }

    if (allocatedMinor != totalSourceDebitMinor) {
      throw ArgumentError.value(
        allocatedMinor,
        'fundingAllocations',
        'must sum to totalSourceDebitMinor',
      );
    }

    if (fundingSources != null) {
      _validateFundingSources(fundingSources);
    }
  }

  void _validateFundingSources(Iterable<FundingSource> fundingSources) {
    final sourcesById = <String, FundingSource>{};

    for (final source in fundingSources) {
      source.validate();

      if (sourcesById.containsKey(source.id)) {
        throw ArgumentError.value(
          source.id,
          'fundingSources.id',
          'must be unique',
        );
      }

      sourcesById[source.id] = source;
    }

    for (final allocation in fundingAllocations) {
      final source = sourcesById[allocation.sourceId];

      if (source == null) {
        throw ArgumentError.value(
          allocation.sourceId,
          'fundingAllocations.sourceId',
          'must identify a funding source',
        );
      }

      if (source.type != allocation.sourceType) {
        throw ArgumentError.value(
          allocation.sourceType,
          'fundingAllocations.sourceType',
          'must match the funding source',
        );
      }

      if (!source.isAvailable) {
        throw ArgumentError.value(
          source.id,
          'fundingAllocations.sourceId',
          'must identify an available funding source',
        );
      }

      if (source.currencyCode != allocation.currencyCode) {
        throw ArgumentError.value(
          source.currencyCode,
          'fundingSources.currencyCode',
          'must match the allocation currency',
        );
      }

      if (source.type == FundingSourceType.wallet &&
          allocation.amountMinor > source.availableMinor!) {
        throw ArgumentError.value(
          allocation.amountMinor,
          'fundingAllocations.amountMinor',
          'must not exceed wallet availableMinor',
        );
      }
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
