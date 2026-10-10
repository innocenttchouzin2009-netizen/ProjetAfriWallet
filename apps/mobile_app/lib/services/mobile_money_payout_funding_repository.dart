import '../data/remote/mobile_money_payout_funding_mapper.dart';
import '../data/remote/mobile_money_payout_funding_remote_data_source.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_funding_plan.dart';

abstract interface class MobileMoneyPayoutFundingRepository {
  Future<MobileMoneyPayoutFundingPlan> planFunding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  });
}

class RemoteMobileMoneyPayoutFundingRepository
    implements MobileMoneyPayoutFundingRepository {
  const RemoteMobileMoneyPayoutFundingRepository(this._remoteDataSource);

  final MobileMoneyPayoutFundingRemoteDataSource _remoteDataSource;

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
    final response = await _remoteDataSource.planFunding(request);

    return MobileMoneyPayoutFundingPlan(
      correlationId: response.correlationId,
      requiredAmountMinor: response.requiredAmountMinor,
      currencyCode: response.currencyCode,
      allocations: MobileMoneyPayoutFundingMapper.allocationsToDomain(response),
      plannedAtUtc: response.plannedAtUtc,
    );
  }
}
