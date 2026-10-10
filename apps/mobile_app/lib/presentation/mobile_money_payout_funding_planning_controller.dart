import 'package:flutter/foundation.dart';

import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_funding_plan.dart';
import '../services/mobile_money_payout_funding_repository.dart';

enum MobileMoneyPayoutFundingPlanningPresentationStatus {
  idle,
  loading,
  ready,
  failed,
}

class MobileMoneyPayoutFundingPlanningController extends ChangeNotifier {
  MobileMoneyPayoutFundingPlanningController({
    required this._repository,
  });

  final MobileMoneyPayoutFundingRepository _repository;

  MobileMoneyPayoutFundingPlanningPresentationStatus _status =
      MobileMoneyPayoutFundingPlanningPresentationStatus.idle;
  MobileMoneyPayoutFundingPlan? _plan;
  Object? _error;

  MobileMoneyPayoutFundingPlanningPresentationStatus get status => _status;
  MobileMoneyPayoutFundingPlan? get plan => _plan;
  Object? get error => _error;

  bool get isLoading =>
      _status == MobileMoneyPayoutFundingPlanningPresentationStatus.loading;
  bool get hasPlan =>
      _status == MobileMoneyPayoutFundingPlanningPresentationStatus.ready &&
      _plan != null;
  bool get hasFailed =>
      _status == MobileMoneyPayoutFundingPlanningPresentationStatus.failed;

  Future<void> planFunding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  }) async {
    if (isLoading) {
      return;
    }

    _status = MobileMoneyPayoutFundingPlanningPresentationStatus.loading;
    _plan = null;
    _error = null;
    notifyListeners();

    try {
      _plan = await _repository.planFunding(
        correlationId: correlationId,
        intent: intent,
        requestedAtUtc: requestedAtUtc,
      );
      _status = MobileMoneyPayoutFundingPlanningPresentationStatus.ready;
    } catch (error) {
      _status = MobileMoneyPayoutFundingPlanningPresentationStatus.failed;
      _plan = null;
      _error = error;
    }

    notifyListeners();
  }

  void reset() {
    if (_status == MobileMoneyPayoutFundingPlanningPresentationStatus.idle &&
        _plan == null &&
        _error == null) {
      return;
    }

    _status = MobileMoneyPayoutFundingPlanningPresentationStatus.idle;
    _plan = null;
    _error = null;
    notifyListeners();
  }
}
