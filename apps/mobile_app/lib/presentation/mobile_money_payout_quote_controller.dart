import 'package:flutter/foundation.dart';

import '../models/mobile_money_payout_quote.dart';
import '../services/mobile_money_payout_quote_repository.dart';

enum MobileMoneyPayoutQuotePresentationStatus {
  idle,
  loading,
  ready,
  failed,
}

class MobileMoneyPayoutQuoteController extends ChangeNotifier {
  MobileMoneyPayoutQuoteController({
    required this._repository,
  });

  final MobileMoneyPayoutQuoteRepository _repository;

  MobileMoneyPayoutQuotePresentationStatus _status =
      MobileMoneyPayoutQuotePresentationStatus.idle;
  MobileMoneyPayoutQuote? _quote;
  Object? _error;

  MobileMoneyPayoutQuotePresentationStatus get status => _status;
  MobileMoneyPayoutQuote? get quote => _quote;
  Object? get error => _error;

  bool get isLoading =>
      _status == MobileMoneyPayoutQuotePresentationStatus.loading;
  bool get hasQuote =>
      _status == MobileMoneyPayoutQuotePresentationStatus.ready &&
      _quote != null;
  bool get hasFailed =>
      _status == MobileMoneyPayoutQuotePresentationStatus.failed;

  Future<void> createQuote({
    required String sourceCountryCode,
    required String sourceCurrencyCode,
    required String destinationCountryCode,
    required String destinationCurrencyCode,
    required String operatorCode,
    required int sourceAmountMinor,
  }) async {
    if (isLoading) {
      return;
    }

    _status = MobileMoneyPayoutQuotePresentationStatus.loading;
    _quote = null;
    _error = null;
    notifyListeners();

    try {
      _quote = await _repository.createQuote(
        sourceCountryCode: sourceCountryCode,
        sourceCurrencyCode: sourceCurrencyCode,
        destinationCountryCode: destinationCountryCode,
        destinationCurrencyCode: destinationCurrencyCode,
        operatorCode: operatorCode,
        sourceAmountMinor: sourceAmountMinor,
      );
      _status = MobileMoneyPayoutQuotePresentationStatus.ready;
    } catch (error) {
      _status = MobileMoneyPayoutQuotePresentationStatus.failed;
      _quote = null;
      _error = error;
    }

    notifyListeners();
  }

  void reset() {
    if (_status == MobileMoneyPayoutQuotePresentationStatus.idle &&
        _quote == null &&
        _error == null) {
      return;
    }

    _status = MobileMoneyPayoutQuotePresentationStatus.idle;
    _quote = null;
    _error = null;
    notifyListeners();
  }
}
