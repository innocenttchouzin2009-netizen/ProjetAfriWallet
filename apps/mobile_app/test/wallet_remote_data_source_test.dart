import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/wallet_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('WalletRemoteDataSource', () {
    test('reads the authenticated mobile wallet projection', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'wallets': <Object?>[
                <String, Object?>{
                  'walletId': '11111111-1111-1111-1111-111111111111',
                  'currency': 'XAF',
                  'availableMinor': 125000,
                  'status': 'ACTIVE',
                  'countryCode': 'CM',
                },
              ],
            }),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      final wallets = await dataSource.loadWalletBalances('access-secret');

      expect(captured.method, 'GET');
      expect(captured.url.path, '/api/v1/wallets/mobile');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(wallets, hasLength(1));
      expect(wallets.single.walletId, '11111111-1111-1111-1111-111111111111');
      expect(wallets.single.currency, 'XAF');
      expect(wallets.single.availableMinor, 125000);
      expect(wallets.single.status, 'ACTIVE');
      expect(wallets.single.countryCode, 'CM');
      apiClient.close();
    });

    test('returns an empty list for an authenticated owner without wallets', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            '{"wallets":[]}',
            200,
            headers: <String, String>{'content-type': 'application/json'},
          ),
        ),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      final wallets = await dataSource.loadWalletBalances('access-secret');

      expect(wallets, isEmpty);
      apiClient.close();
    });

    test('preserves a nullable countryCode', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            '{"wallets":[{"walletId":"11111111-1111-1111-1111-111111111111","currency":"EUR","availableMinor":0,"status":"ACTIVE","countryCode":null}]}',
            200,
          ),
        ),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      final wallets = await dataSource.loadWalletBalances('access-secret');

      expect(wallets.single.countryCode, isNull);
      apiClient.close();
    });

    test('rejects a non-object mobile wallet payload', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('[]', 200)),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      await expectLater(
        dataSource.loadWalletBalances('access-secret'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      apiClient.close();
    });

    test('rejects a missing wallets collection', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('{}', 200)),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      await expectLater(
        dataSource.loadWalletBalances('access-secret'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      apiClient.close();
    });

    test('rejects malformed wallet items', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            '{"wallets":[{"walletId":"only-id"}]}',
            200,
          ),
        ),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      await expectLater(
        dataSource.loadWalletBalances('access-secret'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      apiClient.close();
    });

    test('propagates unauthorized responses without masking them', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      await expectLater(
        dataSource.loadWalletBalances('expired-token'),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      apiClient.close();
    });
  });
}
