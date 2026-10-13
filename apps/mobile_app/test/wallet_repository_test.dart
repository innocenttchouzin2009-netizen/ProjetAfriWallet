import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/wallet_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/authenticated_request_executor.dart';
import 'package:mobile_app/services/wallet_repository.dart';

void main() {
  group('AuthenticatedWalletRepository', () {
    test('loads wallets with the current authenticated access token', () async {
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
        restoredSession: _storedSession('access-secret'),
      );
      final repository = AuthenticatedWalletRepository(
        WalletRemoteDataSource(apiClient),
        AuthenticatedRequestExecutor(lifecycle),
      );

      final wallets = await repository.loadWalletBalances();

      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(wallets, hasLength(1));
      expect(wallets.single.currency, 'XAF');
      expect(wallets.single.availableMinor, 125000);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
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
      final lifecycle = _FakeAuthSessionLifecycle();
      final repository = AuthenticatedWalletRepository(
        WalletRemoteDataSource(apiClient),
        AuthenticatedRequestExecutor(lifecycle),
      );

      await expectLater(
        repository.loadWalletBalances(),
        throwsA(isA<WalletUnavailableException>()),
      );
      expect(remoteCalled, isFalse);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
      apiClient.close();
    });

    test('refreshes once after 401 and retries wallet read', () async {
      final authorizationHeaders = <String?>[];
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          final authorization = request.headers['Authorization'];
          authorizationHeaders.add(authorization);

          if (authorization == 'Bearer access-secret') {
            return http.Response('', 401);
          }

          return http.Response(
            '{"wallets":[]}',
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _storedSession('access-secret'),
        refreshedSession: _storedSession('refreshed-access-secret'),
      );
      final repository = AuthenticatedWalletRepository(
        WalletRemoteDataSource(apiClient),
        AuthenticatedRequestExecutor(lifecycle),
      );

      final wallets = await repository.loadWalletBalances();

      expect(wallets, isEmpty);
      expect(
        authorizationHeaders,
        <String?>[
          'Bearer access-secret',
          'Bearer refreshed-access-secret',
        ],
      );
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 1);
      apiClient.close();
    });
  });
}

StoredAuthSession _storedSession(String accessToken) => StoredAuthSession(
      accessToken: accessToken,
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 10, 13, 18),
    );

class _FakeAuthSessionLifecycle implements AuthSessionLifecycle {
  _FakeAuthSessionLifecycle({
    this.restoredSession,
    this.refreshedSession,
  });

  StoredAuthSession? restoredSession;
  StoredAuthSession? refreshedSession;
  int restoreCalls = 0;
  int refreshCalls = 0;
  int clearCalls = 0;

  @override
  Future<StoredAuthSession?> restoreValidSession() async {
    restoreCalls += 1;
    return restoredSession;
  }

  @override
  Future<StoredAuthSession?> refreshSession() async {
    refreshCalls += 1;
    return refreshedSession;
  }

  @override
  Future<void> clearLocalSession() async {
    clearCalls += 1;
  }
}
