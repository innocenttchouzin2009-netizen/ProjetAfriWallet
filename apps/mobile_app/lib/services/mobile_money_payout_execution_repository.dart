import '../data/remote/mobile_money_payout_execution_dto.dart';
import '../data/remote/mobile_money_payout_execution_remote_data_source.dart';
import '../models/mobile_money_payout_execution_handoff.dart';
import '../models/mobile_money_payout_execution_preparation.dart';
import 'authenticated_request_executor.dart';

abstract interface class MobileMoneyPayoutExecutionRepository {
  Future<MobileMoneyPayoutExecutionPreparation> prepareExecution({
    required String fundingCorrelationId,
    required String idempotencyKey,
    required MobileMoneyPayoutExecutionHandoff handoff,
  });
}

class MobileMoneyPayoutExecutionPreparationMismatchException
    implements Exception {
  const MobileMoneyPayoutExecutionPreparationMismatchException(this.message);

  final String message;

  @override
  String toString() => message;
}

class RemoteMobileMoneyPayoutExecutionRepository
    implements MobileMoneyPayoutExecutionRepository {
  const RemoteMobileMoneyPayoutExecutionRepository(
    this._remoteDataSource,
    this._requestExecutor,
  );

  final MobileMoneyPayoutExecutionRemoteDataSource _remoteDataSource;
  final AuthenticatedRequestExecutor _requestExecutor;

  @override
  Future<MobileMoneyPayoutExecutionPreparation> prepareExecution({
    required String fundingCorrelationId,
    required String idempotencyKey,
    required MobileMoneyPayoutExecutionHandoff handoff,
  }) async {
    handoff.validate();

    if (fundingCorrelationId.trim().isEmpty) {
      throw ArgumentError.value(
        fundingCorrelationId,
        'fundingCorrelationId',
        'must not be empty',
      );
    }

    final normalizedIdempotencyKey = idempotencyKey.trim();
    if (normalizedIdempotencyKey.isEmpty) {
      throw ArgumentError.value(
        idempotencyKey,
        'idempotencyKey',
        'must not be empty',
      );
    }

    final response = await _requestExecutor.execute(
      (accessToken) => _remoteDataSource.prepareExecution(
        accessToken,
        normalizedIdempotencyKey,
        MobileMoneyPayoutExecutionRequestDto(
          quoteId: handoff.quoteId,
          fundingCorrelationId: fundingCorrelationId,
        ),
      ),
    );

    final preparation = MobileMoneyPayoutExecutionPreparation(
      bindingId: response.bindingId,
      quoteId: response.quoteId,
      fundingCorrelationId: response.fundingCorrelationId,
      idempotencyKey: response.idempotencyKey,
      status: response.status,
      isReplay: response.isReplay,
      preparedAtUtc: response.preparedAtUtc,
    );

    _validateResponseBinding(
      fundingCorrelationId: fundingCorrelationId,
      idempotencyKey: normalizedIdempotencyKey,
      handoff: handoff,
      preparation: preparation,
    );
    return preparation;
  }

  void _validateResponseBinding({
    required String fundingCorrelationId,
    required String idempotencyKey,
    required MobileMoneyPayoutExecutionHandoff handoff,
    required MobileMoneyPayoutExecutionPreparation preparation,
  }) {
    if (preparation.quoteId != handoff.quoteId) {
      throw const MobileMoneyPayoutExecutionPreparationMismatchException(
        'The execution preparation quote ID does not match the request.',
      );
    }
    if (preparation.fundingCorrelationId != fundingCorrelationId) {
      throw const MobileMoneyPayoutExecutionPreparationMismatchException(
        'The execution preparation funding correlation ID does not match the request.',
      );
    }
    if (preparation.idempotencyKey != idempotencyKey) {
      throw const MobileMoneyPayoutExecutionPreparationMismatchException(
        'The execution preparation idempotency key does not match the request.',
      );
    }
    if (preparation.status != 'Prepared') {
      throw const MobileMoneyPayoutExecutionPreparationMismatchException(
        'The execution preparation status must be Prepared.',
      );
    }
    if (!preparation.preparedAtUtc.isUtc) {
      throw const MobileMoneyPayoutExecutionPreparationMismatchException(
        'The execution preparation timestamp must be UTC.',
      );
    }
  }
}
