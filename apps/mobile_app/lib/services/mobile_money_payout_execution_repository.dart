import '../data/remote/mobile_money_payout_execution_dto.dart';
import '../data/remote/mobile_money_payout_execution_remote_data_source.dart';
import '../models/mobile_money_payout_execution_handoff.dart';
import '../models/mobile_money_payout_execution_preparation.dart';
import 'secure_session_store.dart';

abstract interface class MobileMoneyPayoutExecutionRepository {
  Future<MobileMoneyPayoutExecutionPreparation> prepareExecution({
    required String fundingCorrelationId,
    required String idempotencyKey,
    required MobileMoneyPayoutExecutionHandoff handoff,
  });
}

class MobileMoneyPayoutExecutionUnavailableException implements Exception {
  const MobileMoneyPayoutExecutionUnavailableException(this.message);

  final String message;

  @override
  String toString() => message;
}

class RemoteMobileMoneyPayoutExecutionRepository
    implements MobileMoneyPayoutExecutionRepository {
  const RemoteMobileMoneyPayoutExecutionRepository(
    this._remoteDataSource,
    this._sessionStore,
  );

  final MobileMoneyPayoutExecutionRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;

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

    if (idempotencyKey.trim().isEmpty) {
      throw ArgumentError.value(
        idempotencyKey,
        'idempotencyKey',
        'must not be empty',
      );
    }

    final session = await _sessionStore.read();
    if (session == null) {
      throw const MobileMoneyPayoutExecutionUnavailableException(
        'An authenticated session is required to prepare mobile money payout execution.',
      );
    }

    final response = await _remoteDataSource.prepareExecution(
      session.accessToken,
      idempotencyKey,
      MobileMoneyPayoutExecutionRequestDto(
        quoteId: handoff.quoteId,
        fundingCorrelationId: fundingCorrelationId,
      ),
    );

    return MobileMoneyPayoutExecutionPreparation(
      bindingId: response.bindingId,
      quoteId: response.quoteId,
      fundingCorrelationId: response.fundingCorrelationId,
      idempotencyKey: response.idempotencyKey,
      status: response.status,
      isReplay: response.isReplay,
      preparedAtUtc: response.preparedAtUtc,
    );
  }
}
