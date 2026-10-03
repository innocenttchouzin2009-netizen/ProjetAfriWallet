import '../../models/mobile_money_payout.dart';
import '../../network/api_client.dart';
import '../../network/api_exception.dart';

class MobileMoneyPayoutEligibilityResponse {
  const MobileMoneyPayoutEligibilityResponse({
    required this.isEligible,
    this.failureCode,
  });

  final bool isEligible;
  final String? failureCode;
}

class MobileMoneyPayoutRemoteDataSource {
  const MobileMoneyPayoutRemoteDataSource(this._apiClient);

  static const String _eligibilityPath =
      '/api/v1/mobile-money/payouts/eligibility';

  final ApiClient _apiClient;

  Future<MobileMoneyPayoutEligibilityResponse> checkEligibility({
    required String sourceCountryCode,
    required MobileMoneyPayoutRequest payout,
  }) async {
    final payload = await _apiClient.postJson(
      _eligibilityPath,
      body: <String, Object?>{
        'sourceCountryCode': sourceCountryCode,
        'sourceCurrency': payout.sendCurrencyCode,
        'destinationCountryCode': payout.beneficiary.countryCode,
        'destinationCurrency': payout.payoutCurrencyCode,
        'operatorCode': payout.beneficiary.operatorCode,
      },
    );

    try {
      final json = _requireObject(payload);
      final isEligible = json['isEligible'];
      if (isEligible is! bool) {
        throw const FormatException('Missing or invalid isEligible.');
      }

      final failureCode = json['failureCode'];
      if (failureCode != null &&
          (failureCode is! String || failureCode.trim().isEmpty)) {
        throw const FormatException('Invalid failureCode.');
      }

      return MobileMoneyPayoutEligibilityResponse(
        isEligible: isEligible,
        failureCode: failureCode as String?,
      );
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The Mobile Money payout eligibility endpoint returned an invalid response.',
      );
    }
  }

  Map<String, dynamic> _requireObject(Object? payload) {
    if (payload is Map<String, dynamic>) {
      return payload;
    }

    throw const FormatException('Expected a JSON object.');
  }
}
