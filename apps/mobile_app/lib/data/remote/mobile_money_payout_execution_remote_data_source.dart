import '../../network/api_client.dart';
import '../../network/api_exception.dart';
import 'mobile_money_payout_execution_dto.dart';
import 'mobile_money_payout_execution_mapper.dart';

class MobileMoneyPayoutExecutionRemoteDataSource {
  const MobileMoneyPayoutExecutionRemoteDataSource(this._apiClient);

  static const String _preparePath =
      '/api/v1/mobile-money/payouts/execution/prepare';

  final ApiClient _apiClient;

  Future<MobileMoneyPayoutExecutionResponseDto> prepareExecution(
    String accessToken,
    String idempotencyKey,
    MobileMoneyPayoutExecutionRequestDto request,
  ) async {
    if (accessToken.trim().isEmpty) {
      throw ArgumentError.value(
        accessToken,
        'accessToken',
        'must not be empty',
      );
    }
    if (idempotencyKey.trim().isEmpty) {
      throw ArgumentError.value(
        idempotencyKey,
        'idempotencyKey',
        'must not be empty',
      );
    }

    final payload = await _apiClient.postJson(
      _preparePath,
      body: MobileMoneyPayoutExecutionMapper.requestToJson(request),
      headers: <String, String>{
        'Authorization': 'Bearer $accessToken',
        'Idempotency-Key': idempotencyKey.trim(),
      },
    );

    try {
      return MobileMoneyPayoutExecutionMapper.responseFromJson(payload);
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The mobile money payout execution endpoint returned an invalid response.',
      );
    }
  }
}
