import 'package:flutter/foundation.dart';

import '../models/mobile_money_payout.dart';
import '../services/mobile_money_payout_repository.dart';

enum MobileMoneyPayoutEligibilityPresentationStatus {
  idle,
  checking,
  eligible,
  ineligible,
  failed,
}

class MobileMoneyPayoutEligibilityController extends ChangeNotifier {
  MobileMoneyPayoutEligibilityController({
    required this._repository,
  });

  final MobileMoneyPayoutRepository _repository;

  MobileMoneyPayoutEligibilityPresentationStatus _status =
      MobileMoneyPayoutEligibilityPresentationStatus.idle;
  String? _failureCode;
  Object? _error;

  MobileMoneyPayoutEligibilityPresentationStatus get status => _status;
  String? get failureCode => _failureCode;
  Object? get error => _error;

  bool get isChecking =>
      _status == MobileMoneyPayoutEligibilityPresentationStatus.checking;
  bool get isEligible =>
      _status == MobileMoneyPayoutEligibilityPresentationStatus.eligible;
  bool get isIneligible =>
      _status == MobileMoneyPayoutEligibilityPresentationStatus.ineligible;
  bool get hasFailed =>
      _status == MobileMoneyPayoutEligibilityPresentationStatus.failed;

  Future<void> checkEligibility({
    required String sourceCountryCode,
    required MobileMoneyPayoutRequest payout,
    Iterable<FundingSource>? fundingSources,
  }) async {
    if (isChecking) {
      return;
    }

    _status = MobileMoneyPayoutEligibilityPresentationStatus.checking;
    _failureCode = null;
    _error = null;
    notifyListeners();

    try {
      final result = await _repository.checkEligibility(
        sourceCountryCode: sourceCountryCode,
        payout: payout,
        fundingSources: fundingSources,
      );

      if (result.isEligible) {
        _status = MobileMoneyPayoutEligibilityPresentationStatus.eligible;
        _failureCode = null;
      } else {
        _status = MobileMoneyPayoutEligibilityPresentationStatus.ineligible;
        _failureCode = result.failureCode;
      }
    } catch (error) {
      _status = MobileMoneyPayoutEligibilityPresentationStatus.failed;
      _failureCode = null;
      _error = error;
    }

    notifyListeners();
  }

  void reset() {
    if (_status == MobileMoneyPayoutEligibilityPresentationStatus.idle &&
        _failureCode == null &&
        _error == null) {
      return;
    }

    _status = MobileMoneyPayoutEligibilityPresentationStatus.idle;
    _failureCode = null;
    _error = null;
    notifyListeners();
  }
}
