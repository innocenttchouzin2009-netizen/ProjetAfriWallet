import '../data/remote/mobile_money_payout_funding_mapper.dart';
import '../data/remote/mobile_money_payout_funding_remote_data_source.dart';
import '../models/mobile_money_payout.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_funding_plan.dart';
import 'authenticated_request_executor.dart';

abstract interface class MobileMoneyPayoutFundingRepository {
  Future<MobileMoneyPayoutFundingPlan> planFunding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  });
}

class MobileMoneyPayoutFundingPlanMismatchException implements Exception {
  const MobileMoneyPayoutFundingPlanMismatchException(this.message);

  final String message;

  @override
  String toString() => message;
}

class RemoteMobileMoneyPayoutFundingRepository
    implements MobileMoneyPayoutFundingRepository {
  const RemoteMobileMoneyPayoutFundingRepository(
    this._remoteDataSource,
    this._requestExecutor,
  );

  final MobileMoneyPayoutFundingRemoteDataSource _remoteDataSource;
  final AuthenticatedRequestExecutor _requestExecutor;

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

    final response = await _requestExecutor.execute(
      (accessToken) => _remoteDataSource.planFunding(
        accessToken,
        request,
      ),
    );

    final plan = MobileMoneyPayoutFundingPlan(
      correlationId: response.correlationId,
      requiredAmountMinor: response.requiredAmountMinor,
      currencyCode: response.currencyCode,
      allocations: MobileMoneyPayoutFundingMapper.allocationsToDomain(response),
      plannedAtUtc: response.plannedAtUtc,
    );

    _validatePlanBinding(
      correlationId: correlationId,
      intent: intent,
      plan: plan,
    );
    return plan;
  }

  void _validatePlanBinding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required MobileMoneyPayoutFundingPlan plan,
  }) {
    if (plan.correlationId != correlationId) {
      throw const MobileMoneyPayoutFundingPlanMismatchException(
        'The funding plan correlation ID does not match the request.',
      );
    }
    if (plan.requiredAmountMinor != intent.totalSourceDebitMinor) {
      throw const MobileMoneyPayoutFundingPlanMismatchException(
        'The funding plan amount does not match the selected funding intent.',
      );
    }
    if (plan.currencyCode != intent.sourceCurrencyCode) {
      throw const MobileMoneyPayoutFundingPlanMismatchException(
        'The funding plan currency does not match the selected funding intent.',
      );
    }
    if (plan.allocations.length != intent.fundingAllocations.length) {
      throw const MobileMoneyPayoutFundingPlanMismatchException(
        'The funding plan allocations do not match the selected funding intent.',
      );
    }

    final expectedBySourceId = <String, FundingAllocation>{
      for (final allocation in intent.fundingAllocations)
        allocation.sourceId: allocation,
    };

    for (final actual in plan.allocations) {
      final expected = expectedBySourceId[actual.sourceId];
      if (expected == null ||
          actual.sourceType != expected.sourceType ||
          actual.amountMinor != expected.amountMinor ||
          actual.currencyCode != expected.currencyCode) {
        throw const MobileMoneyPayoutFundingPlanMismatchException(
          'The funding plan allocations do not match the selected funding intent.',
        );
      }
    }
  }
}
