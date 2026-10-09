import '../../models/mobile_money_payout.dart';
import '../../models/mobile_money_payout_funding_intent.dart';
import 'mobile_money_payout_funding_dto.dart';

class MobileMoneyPayoutFundingMapper {
  const MobileMoneyPayoutFundingMapper._();

  static MobileMoneyPayoutFundingRequestDto requestFromIntent({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  }) {
    intent.validate();
    _requireGuid(correlationId, 'correlationId');
    _requireUtc(requestedAtUtc, 'requestedAtUtc');

    return MobileMoneyPayoutFundingRequestDto(
      correlationId: correlationId,
      requiredAmountMinor: intent.totalSourceDebitMinor,
      currencyCode: intent.sourceCurrencyCode,
      allocations: intent.fundingAllocations
          .map(
            (allocation) => MobileMoneyPayoutFundingAllocationDto(
              sourceId: allocation.sourceId,
              sourceType: _sourceTypeToWire(allocation.sourceType),
              amountMinor: allocation.amountMinor,
              currencyCode: allocation.currencyCode,
            ),
          )
          .toList(growable: false),
      requestedAtUtc: requestedAtUtc,
    );
  }

  static Map<String, Object?> requestToJson(
    MobileMoneyPayoutFundingRequestDto request,
  ) {
    _validateRequest(request);

    return <String, Object?>{
      'correlationId': request.correlationId,
      'requiredAmountMinor': request.requiredAmountMinor,
      'currencyCode': request.currencyCode,
      'allocations': request.allocations
          .map(
            (allocation) => <String, Object?>{
              'sourceId': allocation.sourceId,
              'sourceType': allocation.sourceType,
              'amountMinor': allocation.amountMinor,
              'currencyCode': allocation.currencyCode,
            },
          )
          .toList(growable: false),
      'requestedAtUtc': request.requestedAtUtc.toUtc().toIso8601String(),
    };
  }

  static MobileMoneyPayoutFundingPlanResponseDto responseFromJson(
    Object? payload,
  ) {
    if (payload is! Map<String, dynamic>) {
      throw const FormatException('Expected a payout funding JSON object.');
    }

    final correlationId = _requiredGuid(payload, 'correlationId');
    final requiredAmountMinor = _requiredPositiveInt(
      payload,
      'requiredAmountMinor',
    );
    final currencyCode = _requiredCurrency(payload, 'currencyCode');

    final rawAllocations = payload['allocations'];
    if (rawAllocations is! List || rawAllocations.isEmpty) {
      throw const FormatException(
        'Missing or invalid payout funding allocations.',
      );
    }

    final allocations = rawAllocations
        .map(_allocationFromJson)
        .toList(growable: false);

    final allocatedMinor = allocations.fold<int>(
      0,
      (total, allocation) => total + allocation.amountMinor,
    );
    if (allocatedMinor != requiredAmountMinor) {
      throw const FormatException(
        'Payout funding allocations must sum to requiredAmountMinor.',
      );
    }

    if (allocations.any(
      (allocation) => allocation.currencyCode != currencyCode,
    )) {
      throw const FormatException(
        'Payout funding allocation currency must match currencyCode.',
      );
    }

    final plannedAtUtc = _requiredUtcDateTime(payload, 'plannedAtUtc');

    return MobileMoneyPayoutFundingPlanResponseDto(
      correlationId: correlationId,
      requiredAmountMinor: requiredAmountMinor,
      currencyCode: currencyCode,
      allocations: allocations,
      plannedAtUtc: plannedAtUtc,
    );
  }

  static List<FundingAllocation> allocationsToDomain(
    MobileMoneyPayoutFundingPlanResponseDto response,
  ) {
    return response.allocations
        .map(
          (allocation) => FundingAllocation(
            sourceId: allocation.sourceId,
            sourceType: _sourceTypeFromWire(allocation.sourceType),
            amountMinor: allocation.amountMinor,
            currencyCode: allocation.currencyCode,
          ),
        )
        .toList(growable: false);
  }

  static void _validateRequest(MobileMoneyPayoutFundingRequestDto request) {
    _requireGuid(request.correlationId, 'correlationId');

    if (request.requiredAmountMinor <= 0) {
      throw ArgumentError.value(
        request.requiredAmountMinor,
        'requiredAmountMinor',
        'must be greater than zero',
      );
    }

    _requireCurrency(request.currencyCode, 'currencyCode');
    _requireUtc(request.requestedAtUtc, 'requestedAtUtc');

    if (request.allocations.isEmpty) {
      throw ArgumentError.value(
        request.allocations,
        'allocations',
        'must not be empty',
      );
    }

    var allocatedMinor = 0;
    for (final allocation in request.allocations) {
      _requireNonEmpty(allocation.sourceId, 'allocations.sourceId');
      _sourceTypeFromWire(allocation.sourceType);
      if (allocation.amountMinor <= 0) {
        throw ArgumentError.value(
          allocation.amountMinor,
          'allocations.amountMinor',
          'must be greater than zero',
        );
      }
      _requireCurrency(
        allocation.currencyCode,
        'allocations.currencyCode',
      );
      if (allocation.currencyCode != request.currencyCode) {
        throw ArgumentError.value(
          allocation.currencyCode,
          'allocations.currencyCode',
          'must match currencyCode',
        );
      }
      allocatedMinor += allocation.amountMinor;
    }

    if (allocatedMinor != request.requiredAmountMinor) {
      throw ArgumentError.value(
        allocatedMinor,
        'allocations',
        'must sum to requiredAmountMinor',
      );
    }
  }

  static MobileMoneyPayoutFundingAllocationDto _allocationFromJson(
    Object? payload,
  ) {
    if (payload is! Map<String, dynamic>) {
      throw const FormatException('Invalid payout funding allocation.');
    }

    final sourceType = payload['sourceType'];
    if (sourceType is! int) {
      throw const FormatException(
        'Missing or invalid payout funding sourceType.',
      );
    }
    _sourceTypeFromWire(sourceType);

    return MobileMoneyPayoutFundingAllocationDto(
      sourceId: _requiredNonEmptyString(payload, 'sourceId'),
      sourceType: sourceType,
      amountMinor: _requiredPositiveInt(payload, 'amountMinor'),
      currencyCode: _requiredCurrency(payload, 'currencyCode'),
    );
  }

  static int _sourceTypeToWire(FundingSourceType sourceType) {
    return switch (sourceType) {
      FundingSourceType.wallet => 0,
      FundingSourceType.applePay => 1,
      FundingSourceType.googlePay => 2,
      FundingSourceType.sepa => 3,
      FundingSourceType.paymentCard => 4,
    };
  }

  static FundingSourceType _sourceTypeFromWire(int value) {
    return switch (value) {
      0 => FundingSourceType.wallet,
      1 => FundingSourceType.applePay,
      2 => FundingSourceType.googlePay,
      3 => FundingSourceType.sepa,
      4 => FundingSourceType.paymentCard,
      _ => throw FormatException(
          'Unsupported payout funding sourceType: $value.',
        ),
    };
  }

  static String _requiredGuid(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = _requiredNonEmptyString(payload, key);
    if (!_guidPattern.hasMatch(value)) {
      throw FormatException('Invalid payout funding $key.');
    }
    return value;
  }

  static String _requiredCurrency(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = _requiredNonEmptyString(payload, key);
    if (!RegExp(r'^[A-Z]{3}$').hasMatch(value)) {
      throw FormatException('Invalid payout funding $key.');
    }
    return value;
  }

  static int _requiredPositiveInt(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = payload[key];
    if (value is! int || value <= 0) {
      throw FormatException('Missing or invalid payout funding $key.');
    }
    return value;
  }

  static String _requiredNonEmptyString(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = payload[key];
    if (value is! String || value.trim().isEmpty) {
      throw FormatException('Missing or invalid payout funding $key.');
    }
    return value;
  }

  static DateTime _requiredUtcDateTime(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = payload[key];
    if (value is! String ||
        !(value.endsWith('Z') || value.endsWith('+00:00'))) {
      throw FormatException('Missing or invalid UTC payout funding $key.');
    }

    final parsed = DateTime.tryParse(value);
    if (parsed == null) {
      throw FormatException('Missing or invalid UTC payout funding $key.');
    }

    return parsed.toUtc();
  }

  static final RegExp _guidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}

  static void _requireCurrency(String value, String name) {
    if (!RegExp(r'^[A-Z]{3}$').hasMatch(value)) {
      throw ArgumentError.value(
        value,
        name,
        'must be an uppercase ISO-4217 code',
      );
    }
  }

  static void _requireNonEmpty(String value, String name) {
    if (value.trim().isEmpty) {
      throw ArgumentError.value(value, name, 'must not be empty');
    }
  }

  static void _requireUtc(DateTime value, String name) {
    if (!value.isUtc) {
      throw ArgumentError.value(value, name, 'must be UTC');
    }
  }
}
,
  );

  static void _requireGuid(String value, String name) {
    if (!_guidPattern.hasMatch(value)) {
      throw ArgumentError.value(value, name, 'must be a canonical GUID');
    }
  }

  static void _requireCurrency(String value, String name) {
    if (!RegExp(r'^[A-Z]{3}$').hasMatch(value)) {
      throw ArgumentError.value(
        value,
        name,
        'must be an uppercase ISO-4217 code',
      );
    }
  }

  static void _requireNonEmpty(String value, String name) {
    if (value.trim().isEmpty) {
      throw ArgumentError.value(value, name, 'must not be empty');
    }
  }

  static void _requireUtc(DateTime value, String name) {
    if (!value.isUtc) {
      throw ArgumentError.value(value, name, 'must be UTC');
    }
  }
}
