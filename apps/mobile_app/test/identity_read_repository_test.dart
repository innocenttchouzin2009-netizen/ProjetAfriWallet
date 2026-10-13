import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/identity_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/authenticated_request_executor.dart';
import 'package:mobile_app/services/identity_read_repository.dart';

void main() {
  group('RemoteIdentityReadRepository', () {
    test('uses executor access token for current profile', () async {
      late http.Request captured;
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'userId': 'user-1',
              'identifier': 'user@example.com',
              'createdAtUtc': '2026-10-01T09:30:00Z',
            }),
            200,
          );
        }),
      );
      final lifecycle = _FakeAuthSessionLifecycle(currentSession: _session());
      final repository = RemoteIdentityReadRepository(
        IdentityRemoteDataSource(client),
        AuthenticatedRequestExecutor(lifecycle),
      );

      await repository.loadCurrentProfile();

      expect(captured.headers['Authorization'], 'Bearer test-access-token');
      expect(lifecycle.restoreCount, 1);
      expect(lifecycle.refreshCount, 0);
      client.close();
    });

    test('refreshes once after unauthorized current profile request', () async {
      final seenTokens = <String>[];
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          final authorization = request.headers['Authorization']!;
          seenTokens.add(authorization);
          if (authorization == 'Bearer stale-access-token') {
            return http.Response('{}', 401);
          }
          return http.Response(
            jsonEncode(<String, Object?>{
              'userId': 'user-1',
              'identifier': 'user@example.com',
              'createdAtUtc': '2026-10-01T09:30:00Z',
            }),
            200,
          );
        }),
      );
      final lifecycle = _FakeAuthSessionLifecycle(
        currentSession: _session(accessToken: 'stale-access-token'),
        refreshedSession: _session(accessToken: 'fresh-access-token'),
      );
      final repository = RemoteIdentityReadRepository(
        IdentityRemoteDataSource(client),
        AuthenticatedRequestExecutor(lifecycle),
      );

      final profile = await repository.loadCurrentProfile();

      expect(profile.userId, 'user-1');
      expect(
        seenTokens,
        <String>[
          'Bearer stale-access-token',
          'Bearer fresh-access-token',
        ],
      );
      expect(lifecycle.restoreCount, 1);
      expect(lifecycle.refreshCount, 1);
      client.close();
    });

    test('rejects current profile when executor has no valid session', () async {
      var remoteCalled = false;
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async {
          remoteCalled = true;
          return http.Response('{}', 200);
        }),
      );
      final lifecycle = _FakeAuthSessionLifecycle();
      final repository = RemoteIdentityReadRepository(
        IdentityRemoteDataSource(client),
        AuthenticatedRequestExecutor(lifecycle),
      );

      await expectLater(
        repository.loadCurrentProfile(),
        throwsA(isA<IdentityReadUnavailableException>()),
      );
      expect(remoteCalled, isFalse);
      expect(lifecycle.restoreCount, 1);
      client.close();
    });

    test('public AfWal ID lookup bypasses authenticated executor', () async {
      final lifecycle = _FakeAuthSessionLifecycle();
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response('{"afWalId":"public.user"}', 200),
        ),
      );
      final repository = RemoteIdentityReadRepository(
        IdentityRemoteDataSource(client),
        AuthenticatedRequestExecutor(lifecycle),
      );

      final identity = await repository.loadPublicAfWalId('public.user');

      expect(identity.afWalId, 'public.user');
      expect(lifecycle.restoreCount, 0);
      expect(lifecycle.refreshCount, 0);
      client.close();
    });
  });
}

StoredAuthSession _session({
  String accessToken = 'test-access-token',
}) =>
    StoredAuthSession(
      accessToken: accessToken,
      refreshToken: 'test-refresh-token',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 10, 13, 15),
    );

class _FakeAuthSessionLifecycle implements AuthSessionLifecycle {
  _FakeAuthSessionLifecycle({
    this.currentSession,
    this.refreshedSession,
  });

  StoredAuthSession? currentSession;
  StoredAuthSession? refreshedSession;
  int restoreCount = 0;
  int refreshCount = 0;
  int clearCount = 0;

  @override
  Future<StoredAuthSession?> restoreValidSession() async {
    restoreCount += 1;
    return currentSession;
  }

  @override
  Future<StoredAuthSession?> refreshSession() async {
    refreshCount += 1;
    currentSession = refreshedSession;
    return refreshedSession;
  }

  @override
  Future<void> clearLocalSession() async {
    clearCount += 1;
    currentSession = null;
  }
}
