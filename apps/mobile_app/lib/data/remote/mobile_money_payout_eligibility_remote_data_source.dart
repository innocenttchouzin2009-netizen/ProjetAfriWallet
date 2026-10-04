import '../../network/api_client.dart';
import '../../network/api_exception.dart';
import 'mobile_money_payout_eligibility_dto.dart';
import 'mobile_money_payout_eligibility_mapper.dart';

class MobileMoneyPayoutEligibilityRemoteDataSource {
  const MobileMoneyPayoutEligibilityRemoteDataSource(this._apiClient);

  static const String _eligibilityPath =
      '/api/v1/mobile-money/payouts/eligibility';

  final ApiClient _apiClient;

  Future<MobileMoneyPayoutEligibilityResponseDto> checkEligibility(
    MobileMoneyPayoutEligibilityRequestDto request,
  ) async {
    final payload = await _apiClient.postJson(
      _eligibilityPath,
      body: MobileMoneyPayoutEligibilityMapper.requestToJson(request),
    );

    try {
      return MobileMoneyPayoutEligibilityMapper.responseFromJson(payload);
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The mobile money payout eligibility endpoint returned an invalid response.',
      );
    }
  }
}
