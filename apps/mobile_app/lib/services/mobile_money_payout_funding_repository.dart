import '../data/remote/mobile_money_payout_funding_mapper.dart';
import '../data/remote/mobile_money_payout_funding_remote_data_source.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_funding_plan.dart';
import 'secure_session_store.dart';

abstract interface class MobileMoneyPayoutFundingRepository {
  Future<MobileMoneyPayoutFundingPlan> planFunding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  });
}

class MobileMoneyPayoutFundingUnavailableException implements Exception {
  const MobileMoneyPayoutFundingUnavailableException(this.message);

  final String message;

  @override
  String toString() => message;
}

class RemoteMobileMoneyPayoutFundingRepository
    implements MobileMoneyPayoutFundingRepository {
  const RemoteMobileMoneyPayoutFundingRepository(
    this._remoteDataSource,
    this._sessionStore,
  );

  final MobileMoneyPayoutFundingRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;

  @override
  Future<MobileMoneyPayoutFundingPlan> planFunding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  }) async {
    final request = MobileMoneyPayoutFundingMapper.requestFromIntent(
      correlationId: correlationId,
      intent: intent,
      requestedAtUtc: requestedAtUtc,
    );
    final session = await _sessionStore.read();
    if (session == null) {
      throw const MobileMoneyPayoutFundingUnavailableException(
        'An authenticated session is required to plan mobile money payout funding.',
      );
    }

    final response = await _remoteDataSource.planFunding(
      session.accessToken,
      request,
    );

    return MobileMoneyPayoutFundingPlan(
      correlationId: response.correlationId,
      requiredAmountMinor: response.requiredAmountMinor,
      currencyCode: response.currencyCode,
      allocations: MobileMoneyPayoutFundingMapper.allocationsToDomain(response),
      plannedAtUtc: response.plannedAtUtc,
    );
  }
}
