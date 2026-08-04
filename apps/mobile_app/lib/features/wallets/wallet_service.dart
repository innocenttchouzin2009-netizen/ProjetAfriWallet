import 'dart:convert';
import 'package:http/http.dart' as http;
import 'wallet_models.dart';

class WalletApiClient {
  WalletApiClient({String? baseUrl})
      : _baseUrl = baseUrl ??
            const String.fromEnvironment(
              'AFW_API_BASE_URL',
              defaultValue: 'http://10.0.2.2:5000',
            );

  final String _baseUrl;

  Future<List<WalletSummary>> listWallets(String awid) async {
    final response = await http.get(Uri.parse('$_baseUrl/api/v1/wallets?awid=$awid'));
    if (response.statusCode != 200) {
      throw Exception('Failed to load wallets');
    }

    final body = jsonDecode(response.body) as Map<String, dynamic>;
    final items = body['items'] as List<dynamic>;
    return items.map((item) => WalletSummary.fromJson(item as Map<String, dynamic>)).toList();
  }

  Future<WalletDetail> getWalletDetail(String walletId) async {
    final response = await http.get(Uri.parse('$_baseUrl/api/v1/wallets/$walletId/read-model'));
    if (response.statusCode != 200) {
      throw Exception('Failed to load wallet detail');
    }

    return WalletDetail.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<WalletPortfolioSummary> getPortfolioSummary(String awid) async {
    final response = await http.get(Uri.parse('$_baseUrl/api/v1/wallets/portfolio/$awid'));
    if (response.statusCode != 200) {
      throw Exception('Failed to load portfolio');
    }

    return WalletPortfolioSummary.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<WalletSummary> createWallet({
    required String awid,
    required String walletType,
    required String currency,
    String? name,
  }) async {
    final response = await http.post(
      Uri.parse('$_baseUrl/api/v1/wallets'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'awid': awid,
        'walletType': walletType,
        'currency': currency,
        'name': name,
      }),
    );
    if (response.statusCode != 201) {
      throw Exception('Failed to create wallet');
    }

    return WalletSummary.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Map<String, dynamic>> createFxQuote({
    required String from,
    required String to,
    required int amountMinor,
  }) async {
    final response = await http.post(
      Uri.parse('$_baseUrl/api/v1/fx/quotes'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'from': from,
        'to': to,
        'amountMinor': amountMinor,
      }),
    );
    if (response.statusCode != 200) {
      throw Exception('Failed to create FX quote');
    }

    return jsonDecode(response.body) as Map<String, dynamic>;
  }
}
