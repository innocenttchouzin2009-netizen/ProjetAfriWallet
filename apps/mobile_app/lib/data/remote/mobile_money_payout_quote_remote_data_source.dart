import '../../network/api_client.dart';
import '../../network/api_exception.dart';
import 'mobile_money_payout_quote_dto.dart';
import 'mobile_money_payout_quote_mapper.dart';

class MobileMoneyPayoutQuoteRemoteDataSource {
  const MobileMoneyPayoutQuoteRemoteDataSource(this._apiClient);

  static const String _quotePath = '/api/v1/mobile-money/payouts/quote';

  final ApiClient _apiClient;

  Future<MobileMoneyPayoutQuoteResponseDto> createQuote(
    MobileMoneyPayoutQuoteRequestDto request,
  ) async {
    final payload = await _apiClient.postJson(
      _quotePath,
      body: MobileMoneyPayoutQuoteMapper.requestToJson(request),
    );

    try {
      return MobileMoneyPayoutQuoteMapper.responseFromJson(payload);
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The mobile money payout quote endpoint returned an invalid response.',
      );
    }
  }
}
