import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/wallet_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('WalletRemoteDataSource', () {
    test('lists owned wallets through the protected backend contract', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<Object?>[
              <String, Object?>{
                'walletId': '11111111-1111-1111-1111-111111111111',
                'ownerId': '22222222-2222-2222-2222-222222222222',
                'currencyCode': 'EUR',
                'countryCode': 'DE',
                'status': 'ACTIVE',
                'createdAtUtc': '2026-09-24T10:00:00Z',
                'updatedAtUtc': '2026-09-24T11:00:00Z',
              },
            ]),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      final wallets = await dataSource.listWallets('access-secret');

      expect(captured.method, 'GET');
      expect(captured.url.path, '/api/v1/wallets');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(wallets, hasLength(1));
      expect(wallets.single.currencyCode, 'EUR');
      apiClient.close();
    });

    test('returns an empty list when the backend has no owned wallets', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('[]', 200)),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      final wallets = await dataSource.listWallets('access-secret');

      expect(wallets, isEmpty);
      apiClient.close();
    });

    test('rejects a non-list wallet payload', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('{}', 200)),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      await expectLater(
        dataSource.listWallets('access-secret'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      apiClient.close();
    });

    test('rejects malformed wallet items', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            '[{"walletId":"only-id"}]',
            200,
          ),
        ),
      );
      final dataSource = WalletRemoteDataSource(apiClient);

      await expectLater(
        dataSource.listWallets('access-secret'),
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
        dataSource.listWallets('expired-token'),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      apiClient.close();
    });
  });
}
