import 'package:flutter/foundation.dart';

import '../models/mobile_money_payout_execution_handoff.dart';
import '../models/mobile_money_payout_execution_preparation.dart';
import '../services/mobile_money_payout_execution_repository.dart';

enum MobileMoneyPayoutExecutionPreparationPresentationStatus {
  idle,
  loading,
  ready,
  failed,
}

class MobileMoneyPayoutExecutionPreparationController extends ChangeNotifier {
  MobileMoneyPayoutExecutionPreparationController({
    required this._repository,
  });

  final MobileMoneyPayoutExecutionRepository _repository;

  MobileMoneyPayoutExecutionPreparationPresentationStatus _status =
      MobileMoneyPayoutExecutionPreparationPresentationStatus.idle;
  MobileMoneyPayoutExecutionPreparation? _preparation;
  Object? _error;

  MobileMoneyPayoutExecutionPreparationPresentationStatus get status => _status;
  MobileMoneyPayoutExecutionPreparation? get preparation => _preparation;
  Object? get error => _error;

  bool get isLoading =>
      _status ==
      MobileMoneyPayoutExecutionPreparationPresentationStatus.loading;
  bool get hasPreparation =>
      _status == MobileMoneyPayoutExecutionPreparationPresentationStatus.ready &&
      _preparation != null;
  bool get hasFailed =>
      _status ==
      MobileMoneyPayoutExecutionPreparationPresentationStatus.failed;

  Future<void> prepareExecution({
    required String fundingCorrelationId,
    required String idempotencyKey,
    required MobileMoneyPayoutExecutionHandoff handoff,
  }) async {
    if (isLoading) return;

    _status = MobileMoneyPayoutExecutionPreparationPresentationStatus.loading;
    _preparation = null;
    _error = null;
    notifyListeners();

    try {
      _preparation = await _repository.prepareExecution(
        fundingCorrelationId: fundingCorrelationId,
        idempotencyKey: idempotencyKey,
        handoff: handoff,
      );
      _status = MobileMoneyPayoutExecutionPreparationPresentationStatus.ready;
    } catch (error) {
      _preparation = null;
      _error = error;
      _status = MobileMoneyPayoutExecutionPreparationPresentationStatus.failed;
    }

    notifyListeners();
  }

  void reset() {
    if (_status == MobileMoneyPayoutExecutionPreparationPresentationStatus.idle &&
        _preparation == null &&
        _error == null) {
      return;
    }
    _status = MobileMoneyPayoutExecutionPreparationPresentationStatus.idle;
    _preparation = null;
    _error = null;
    notifyListeners();
  }
}
