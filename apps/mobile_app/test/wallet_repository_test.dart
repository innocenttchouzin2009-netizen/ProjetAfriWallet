import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/wallet_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/wallet_repository.dart';

void main() {
  group('AuthenticatedWalletRepository', () {
    test('loads wallets with the lifecycle-restored access token', () async {
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
      final lifecycle = _FakeAuthSessionLifecycle(
        _storedSession(accessToken: 'access-fresh'),
      );
      final repository = AuthenticatedWalletRepository.withSessionLifecycle(
        WalletRemoteDataSource(apiClient),
        lifecycle,
      );

      final wallets = await repository.loadWalletBalances();

      expect(lifecycle.restoreCalls, 1);
      expect(captured.headers['Authorization'], 'Bearer access-fresh');
      expect(wallets, hasLength(1));
      expect(wallets.single.currency, 'XAF');
      expect(wallets.single.availableMinor, 125000);
      apiClient.close();
    });

    test('rejects wallet reads when lifecycle cannot restore a session', () async {
      var remoteCalled = false;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async {
          remoteCalled = true;
          return http.Response('{"wallets":[]}', 200);
        }),
      );
      final lifecycle = _FakeAuthSessionLifecycle(null);
      final repository = AuthenticatedWalletRepository.withSessionLifecycle(
        WalletRemoteDataSource(apiClient),
        lifecycle,
      );

      await expectLater(
        repository.loadWalletBalances(),
        throwsA(isA<WalletUnavailableException>()),
      );
      expect(lifecycle.restoreCalls, 1);
      expect(remoteCalled, isFalse);
      apiClient.close();
    });

    test('never sends a wallet request with an expired stored token', () async {
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
        _FakeAuthSessionStore(
          _storedSession(
            accessToken: 'access-expired',
            expiresAtUtc: DateTime.utc(2000),
          ),
        ),
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
      final repository = AuthenticatedWalletRepository.withSessionLifecycle(
        WalletRemoteDataSource(apiClient),
        _FakeAuthSessionLifecycle(_storedSession()),
      );

      await expectLater(
        repository.loadWalletBalances(),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      apiClient.close();
    });
  });
}

StoredAuthSession _storedSession({
  String accessToken = 'access-secret',
  DateTime? expiresAtUtc,
}) =>
    StoredAuthSession(
      accessToken: accessToken,
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: expiresAtUtc ?? DateTime.utc(2100),
    );

class _FakeAuthSessionLifecycle implements AuthSessionLifecycle {
  _FakeAuthSessionLifecycle(this.session);

  StoredAuthSession? session;
  int restoreCalls = 0;

  @override
  Future<StoredAuthSession?> restoreValidSession() async {
    restoreCalls += 1;
    return session;
  }

  @override
  Future<StoredAuthSession?> refreshSession() async => session;

  @override
  Future<void> clearLocalSession() async {
    session = null;
  }
}

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
