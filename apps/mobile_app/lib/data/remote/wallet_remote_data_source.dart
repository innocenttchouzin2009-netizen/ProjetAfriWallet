import '../../models/wallet_balance.dart';
import '../../network/api_client.dart';
import '../../network/api_exception.dart';

class WalletRemoteDataSource {
  const WalletRemoteDataSource(this._apiClient);

  static const String _mobileWalletsPath = '/api/v1/wallets/mobile';

  final ApiClient _apiClient;

  Future<List<WalletBalance>> loadWalletBalances(String accessToken) async {
    final payload = await _apiClient.getJson(
      _mobileWalletsPath,
      headers: <String, String>{
        'Authorization': 'Bearer $accessToken',
      },
    );

    if (payload is! Map<String, dynamic>) {
      throw const ApiMalformedResponseException(
        'The mobile wallet endpoint returned an unexpected payload.',
      );
    }

    final wallets = payload['wallets'];
    if (wallets is! List<dynamic>) {
      throw const ApiMalformedResponseException(
        'The mobile wallet endpoint returned an invalid wallets collection.',
      );
    }

    return wallets.map(_parseWalletBalance).toList(growable: false);
  }

  WalletBalance _parseWalletBalance(Object? value) {
    if (value is! Map<String, dynamic>) {
      throw const ApiMalformedResponseException(
        'The mobile wallet endpoint returned an invalid wallet item.',
      );
    }

    try {
      final countryCode = value['countryCode'];
      if (countryCode != null && countryCode is! String) {
        throw const FormatException('Invalid countryCode.');
      }

      return WalletBalance(
        walletId: _requireString(value, 'walletId'),
        currency: _requireString(value, 'currency'),
        availableMinor: _requireInt(value, 'availableMinor'),
        status: _requireString(value, 'status'),
        countryCode: countryCode as String?,
      );
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The mobile wallet endpoint returned an invalid wallet item.',
      );
    }
  }

  String _requireString(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is String && value.trim().isNotEmpty) {
      return value;
    }

    throw FormatException('Missing or invalid $key.');
  }

  int _requireInt(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is int) {
      return value;
    }

    throw FormatException('Missing or invalid $key.');
  }
}
