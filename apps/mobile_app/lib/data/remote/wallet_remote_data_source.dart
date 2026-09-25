import '../../models/wallet_contracts.dart';
import '../../network/api_client.dart';
import '../../network/api_exception.dart';

class WalletRemoteDataSource {
  const WalletRemoteDataSource(this._apiClient);

  static const String _walletsPath = '/api/v1/wallets';

  final ApiClient _apiClient;

  Future<List<WalletResponse>> listWallets(String accessToken) async {
    final payload = await _apiClient.getJson(
      _walletsPath,
      headers: <String, String>{
        'Authorization': 'Bearer $accessToken',
      },
    );

    if (payload is! List<dynamic>) {
      throw const ApiMalformedResponseException(
        'The wallet endpoint returned an unexpected payload.',
      );
    }

    return payload.map(_parseWallet).toList(growable: false);
  }

  WalletResponse _parseWallet(Object? value) {
    if (value is! Map<String, dynamic>) {
      throw const ApiMalformedResponseException(
        'The wallet endpoint returned an invalid wallet item.',
      );
    }

    try {
      return WalletResponse.fromJson(
        Map<String, Object?>.from(value),
      );
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The wallet endpoint returned an invalid wallet item.',
      );
    }
  }
}
