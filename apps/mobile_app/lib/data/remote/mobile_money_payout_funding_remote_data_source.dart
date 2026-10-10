import '../../network/api_client.dart';
import '../../network/api_exception.dart';
import 'mobile_money_payout_funding_dto.dart';
import 'mobile_money_payout_funding_mapper.dart';

class MobileMoneyPayoutFundingRemoteDataSource {
  const MobileMoneyPayoutFundingRemoteDataSource(this._apiClient);

  static const String _planPath =
      '/api/v1/mobile-money/payouts/funding/plan';

  final ApiClient _apiClient;

  Future<MobileMoneyPayoutFundingPlanResponseDto> planFunding(
    MobileMoneyPayoutFundingRequestDto request,
  ) async {
    final payload = await _apiClient.postJson(
      _planPath,
      body: MobileMoneyPayoutFundingMapper.requestToJson(request),
    );

    try {
      return MobileMoneyPayoutFundingMapper.responseFromJson(payload);
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The mobile money payout funding endpoint returned an invalid response.',
      );
    }
  }
}
