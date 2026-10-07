import 'package:flutter/foundation.dart';

import '../services/mobile_money_beneficiary_lookup_repository.dart';

enum MobileMoneyBeneficiarySearchStatus {
  idle,
  searching,
  resolved,
  manualEntryRequired,
  failed,
}

class MobileMoneyBeneficiarySearchController extends ChangeNotifier {
  MobileMoneyBeneficiarySearchController({
    required this._repository,
  });

  final MobileMoneyBeneficiaryLookupRepository _repository;

  MobileMoneyBeneficiarySearchStatus _status =
      MobileMoneyBeneficiarySearchStatus.idle;
  MobileMoneyBeneficiarySearchResult? _result;
  Object? _error;

  MobileMoneyBeneficiarySearchStatus get status => _status;
  MobileMoneyBeneficiarySearchResult? get result => _result;
  Object? get error => _error;

  bool get isSearching =>
      _status == MobileMoneyBeneficiarySearchStatus.searching;
  bool get isResolved =>
      _status == MobileMoneyBeneficiarySearchStatus.resolved;
  bool get requiresManualEntry =>
      _status == MobileMoneyBeneficiarySearchStatus.manualEntryRequired;
  bool get hasFailed =>
      _status == MobileMoneyBeneficiarySearchStatus.failed;

  Future<void> search({
    required String phoneNumber,
  }) async {
    if (isSearching) {
      return;
    }

    _status = MobileMoneyBeneficiarySearchStatus.searching;
    _result = null;
    _error = null;
    notifyListeners();

    try {
      final searchResult = await _repository.search(
        phoneNumber: phoneNumber,
      );
      _result = searchResult;
      _status = searchResult.isFullyResolved
          ? MobileMoneyBeneficiarySearchStatus.resolved
          : MobileMoneyBeneficiarySearchStatus.manualEntryRequired;
    } catch (error) {
      _status = MobileMoneyBeneficiarySearchStatus.failed;
      _result = null;
      _error = error;
    }

    notifyListeners();
  }

  void reset() {
    if (_status == MobileMoneyBeneficiarySearchStatus.idle &&
        _result == null &&
        _error == null) {
      return;
    }

    _status = MobileMoneyBeneficiarySearchStatus.idle;
    _result = null;
    _error = null;
    notifyListeners();
  }
}
