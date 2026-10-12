class MobileMoneyPayoutExecutionRequestDto {
  const MobileMoneyPayoutExecutionRequestDto({
    required this.quoteId,
    required this.fundingCorrelationId,
  });

  final String quoteId;
  final String fundingCorrelationId;
}

class MobileMoneyPayoutExecutionResponseDto {
  const MobileMoneyPayoutExecutionResponseDto({
    required this.bindingId,
    required this.quoteId,
    required this.fundingCorrelationId,
    required this.idempotencyKey,
    required this.status,
    required this.isReplay,
    required this.preparedAtUtc,
  });

  final String bindingId;
  final String quoteId;
  final String fundingCorrelationId;
  final String idempotencyKey;
  final String status;
  final bool isReplay;
  final DateTime preparedAtUtc;
}
