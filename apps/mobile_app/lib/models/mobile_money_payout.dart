enum FundingSourceType { wallet, applePay, googlePay, sepa, paymentCard }

class MobileMoneyBeneficiary {
  const MobileMoneyBeneficiary({
    this.beneficiaryId,
    required this.displayName,
    required this.phoneNumberE164,
    required this.countryCode,
    required this.operatorCode,
    required this.currencyCode,
  });

  final String? beneficiaryId;
  final String displayName;
  final String phoneNumberE164;
  final String countryCode;
  final String operatorCode;
  final String currencyCode;

  void validate() {
    if (beneficiaryId != null) {
      _requireNonEmpty(beneficiaryId!, 'beneficiaryId');
    }
    _requireNonEmpty(displayName, 'displayName');
    _requireNonEmpty(operatorCode, 'operatorCode');
    _validateCountryCode(countryCode);
    _validateCurrencyCode(currencyCode, 'currencyCode');

    if (!RegExp(r'^\+[1-9][0-9]{1,14}$').hasMatch(phoneNumberE164)) {
      throw ArgumentError.value(
        phoneNumberE164,
        'phoneNumberE164',
        'must be a valid E.164 phone number',
      );
    }
  }
}

class FundingSource {
  const FundingSource({
    required this.id,
    required this.type,
    required this.displayLabel,
    required this.currencyCode,
    required this.isAvailable,
    this.availableMinor,
  });

  final String id;
  final FundingSourceType type;
  final String displayLabel;
  final String currencyCode;
  final bool isAvailable;
  final int? availableMinor;

  void validate() {
    _requireNonEmpty(id, 'id');
    _requireNonEmpty(displayLabel, 'displayLabel');
    _validateCurrencyCode(currencyCode, 'currencyCode');

    if (availableMinor != null && availableMinor! < 0) {
      throw ArgumentError.value(
        availableMinor,
        'availableMinor',
        'must not be negative',
      );
    }

    if (type == FundingSourceType.wallet && availableMinor == null) {
      throw ArgumentError.value(
        availableMinor,
        'availableMinor',
        'is required for a wallet source',
      );
    }

    if (type != FundingSourceType.wallet && availableMinor != null) {
      throw ArgumentError.value(
        availableMinor,
        'availableMinor',
        'is only supported for a wallet source',
      );
    }
  }
}

class FundingAllocation {
  const FundingAllocation({
    required this.sourceId,
    required this.sourceType,
    required this.amountMinor,
    required this.currencyCode,
  });

  final String sourceId;
  final FundingSourceType sourceType;
  final int amountMinor;
  final String currencyCode;

  void validate() {
    _requireNonEmpty(sourceId, 'sourceId');
    if (amountMinor <= 0) {
      throw ArgumentError.value(
        amountMinor,
        'amountMinor',
        'must be greater than zero',
      );
    }
    _validateCurrencyCode(currencyCode, 'currencyCode');
  }
}

class MobileMoneyPayoutRequest {
  const MobileMoneyPayoutRequest({
    required this.beneficiary,
    required this.sendAmountMinor,
    required this.sendCurrencyCode,
    required this.payoutAmountMinor,
    required this.payoutCurrencyCode,
    required this.fundingAllocations,
    required this.idempotencyKey,
    this.quoteId,
    this.message,
  });

  final MobileMoneyBeneficiary beneficiary;
  final int sendAmountMinor;
  final String sendCurrencyCode;
  final int payoutAmountMinor;
  final String payoutCurrencyCode;
  final List<FundingAllocation> fundingAllocations;
  final String idempotencyKey;
  final String? quoteId;
  final String? message;

  void validate({Iterable<FundingSource>? fundingSources}) {
    beneficiary.validate();
    _requirePositive(sendAmountMinor, 'sendAmountMinor');
    _requirePositive(payoutAmountMinor, 'payoutAmountMinor');
    _validateCurrencyCode(sendCurrencyCode, 'sendCurrencyCode');
    _validateCurrencyCode(payoutCurrencyCode, 'payoutCurrencyCode');
    _requireNonEmpty(idempotencyKey, 'idempotencyKey');

    if (quoteId != null) {
      _requireNonEmpty(quoteId!, 'quoteId');
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
      if (allocation.currencyCode != sendCurrencyCode) {
        throw ArgumentError.value(
          allocation.currencyCode,
          'fundingAllocations.currencyCode',
          'must match sendCurrencyCode',
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

    if (allocatedMinor != sendAmountMinor) {
      throw ArgumentError.value(
        allocatedMinor,
        'fundingAllocations',
        'must sum to sendAmountMinor',
      );
    }

    final allocationsBySource = {
      for (final allocation in fundingAllocations)
        allocation.sourceId: allocation,
    };
    if (allocationsBySource.values.any(
      (allocation) => allocation.sourceType == FundingSourceType.wallet,
    )) {
      if (fundingSources == null) {
        throw ArgumentError.value(
          fundingSources,
          'fundingSources',
          'is required to validate wallet availability',
        );
      }
      _validateWalletAvailability(allocationsBySource, fundingSources);
    }
  }

  void _validateWalletAvailability(
    Map<String, FundingAllocation> allocationsBySource,
    Iterable<FundingSource> fundingSources,
  ) {
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

    for (final allocation in allocationsBySource.values) {
      final source = sourcesById[allocation.sourceId];
      if (source == null) {
        throw ArgumentError.value(
          allocation.sourceId,
          'fundingAllocations.sourceId',
          'must identify an available funding source',
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

void _requirePositive(int value, String name) {
  if (value <= 0) {
    throw ArgumentError.value(value, name, 'must be greater than zero');
  }
}

void _requireNonEmpty(String value, String name) {
  if (value.trim().isEmpty) {
    throw ArgumentError.value(value, name, 'must not be empty');
  }
}

void _validateCountryCode(String value) {
  if (!RegExp(r'^[A-Z]{2}$').hasMatch(value)) {
    throw ArgumentError.value(
      value,
      'countryCode',
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
