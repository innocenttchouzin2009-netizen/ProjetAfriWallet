import 'mobile_money_payout_execution_dto.dart';

class MobileMoneyPayoutExecutionMapper {
  const MobileMoneyPayoutExecutionMapper._();

  static Map<String, Object?> requestToJson(
    MobileMoneyPayoutExecutionRequestDto request,
  ) {
    _requireUuid(request.quoteId, 'quoteId');
    _requireUuid(request.fundingCorrelationId, 'fundingCorrelationId');

    return <String, Object?>{
      'quoteId': request.quoteId,
      'fundingCorrelationId': request.fundingCorrelationId,
    };
  }

  static MobileMoneyPayoutExecutionResponseDto responseFromJson(
    Object? payload,
  ) {
    if (payload is! Map<String, dynamic>) {
      throw const FormatException('Expected an execution response object.');
    }

    final bindingId = _requiredString(payload, 'bindingId');
    final quoteId = _requiredString(payload, 'quoteId');
    final fundingCorrelationId = _requiredString(
      payload,
      'fundingCorrelationId',
    );
    final idempotencyKey = _requiredString(payload, 'idempotencyKey');
    final status = _requiredString(payload, 'status');
    final isReplay = payload['isReplay'];
    final preparedAtRaw = _requiredString(payload, 'preparedAtUtc');

    _requireUuid(bindingId, 'bindingId');
    _requireUuid(quoteId, 'quoteId');
    _requireUuid(fundingCorrelationId, 'fundingCorrelationId');

    if (isReplay is! bool) {
      throw const FormatException('isReplay must be a boolean.');
    }

    final preparedAt = DateTime.tryParse(preparedAtRaw);
    if (preparedAt == null) {
      throw const FormatException('preparedAtUtc must be an ISO-8601 instant.');
    }

    return MobileMoneyPayoutExecutionResponseDto(
      bindingId: bindingId,
      quoteId: quoteId,
      fundingCorrelationId: fundingCorrelationId,
      idempotencyKey: idempotencyKey,
      status: status,
      isReplay: isReplay,
      preparedAtUtc: preparedAt.toUtc(),
    );
  }

  static String _requiredString(Map<String, dynamic> payload, String key) {
    final value = payload[key];
    if (value is! String || value.trim().isEmpty) {
      throw FormatException('$key must be a non-empty string.');
    }
    return value;
  }

  static void _requireUuid(String value, String name) {
    if (!_uuidPattern.hasMatch(value)) {
      throw FormatException('$name must be a canonical UUID.');
    }
  }

  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-'
    r'[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
  );
}
