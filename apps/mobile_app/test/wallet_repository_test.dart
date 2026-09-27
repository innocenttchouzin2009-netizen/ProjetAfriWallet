import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/wallet_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/wallet_repository.dart';

void main() {
  group('AuthenticatedWalletRepository', () {
    test('loads wallets with the access token from the stored session', () async {
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
      final repository = AuthenticatedWalletRepository(
        WalletRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );

      final wallets = await repository.loadWalletBalances();

      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(wallets, hasLength(1));
      expect(wallets.single.currency, 'XAF');
      expect(wallets.single.availableMinor, 125000);
      apiClient.close();
    });

    test('rejects wallet reads when no authenticated session exists', () async {
      var remoteCalled = false;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async {
          remoteCalled = true;
          return http.Response('{"wallets":[]}', 200);
        }),
      );
      final repository = AuthenticatedWalletRepository(
        WalletRemoteDataSource(apiClient),
        _FakeAuthSessionStore(null),
      );

      await expectLater(
        repository.loadWalletBalances(),
        throwsA(isA<WalletUnavailableException>()),
      );
      expect(remoteCalled, isFalse);
      apiClient.close();
    });

    test('preserves unauthorized responses from the wallet endpoint', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final repository = AuthenticatedWalletRepository(
        WalletRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );

      await expectLater(
        repository.loadWalletBalances(),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      apiClient.close();
    });
  });
}

StoredAuthSession _storedSession() => StoredAuthSession(
      accessToken: 'access-secret',
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 27, 12),
    );

class _FakeAuthSessionStore implements AuthSessionStore {
  _FakeAuthSessionStore(this.session);

  StoredAuthSession? session;

  @override
  Future<StoredAuthSession?> read() async => session;

  @override
  Future<void> save(StoredAuthSession session) async {
    this.session = session;
  }

  @override
  Future<void> clear() async {
    session = null;
  }
}
