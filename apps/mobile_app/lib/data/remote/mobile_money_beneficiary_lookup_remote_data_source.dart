import '../../network/api_client.dart';
import '../../network/api_exception.dart';
import 'mobile_money_beneficiary_lookup_dto.dart';
import 'mobile_money_beneficiary_lookup_mapper.dart';

class MobileMoneyBeneficiaryLookupRemoteDataSource {
  const MobileMoneyBeneficiaryLookupRemoteDataSource(this._apiClient);

  static const String lookupPath =
      '/api/v1/mobile-money/beneficiaries/lookup';

  final ApiClient _apiClient;

  Future<MobileMoneyBeneficiaryLookupResponseDto> lookup(
    MobileMoneyBeneficiaryLookupRequestDto request,
  ) async {
    final payload = await _apiClient.postJson(
      lookupPath,
      body: MobileMoneyBeneficiaryLookupMapper.requestToJson(request),
    );

    try {
      return MobileMoneyBeneficiaryLookupMapper.responseFromJson(payload);
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The mobile money beneficiary lookup endpoint returned an invalid response.',
      );
    }
  }
}
