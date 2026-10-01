import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/identity_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/identity_read_repository.dart';
import 'package:mobile_app/services/secure_session_store.dart';

void main() {
  group('RemoteIdentityReadRepository', () {
    test('uses stored access token for current profile', () async {
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
      final repository = RemoteIdentityReadRepository(
        IdentityRemoteDataSource(client),
        _FakeAuthSessionStore(_session()),
      );

      await repository.loadCurrentProfile();

      expect(captured.headers['Authorization'], 'Bearer test-access-token');
      client.close();
    });

    test('rejects current profile when session is absent', () async {
      var remoteCalled = false;
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async {
          remoteCalled = true;
          return http.Response('{}', 200);
        }),
      );
      final repository = RemoteIdentityReadRepository(
        IdentityRemoteDataSource(client),
        _FakeAuthSessionStore(null),
      );

      await expectLater(
        repository.loadCurrentProfile(),
        throwsA(isA<IdentityReadUnavailableException>()),
      );
      expect(remoteCalled, isFalse);
      client.close();
    });

    test('public AfWal ID lookup does not read session', () async {
      final store = _FakeAuthSessionStore(null);
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response('{"afWalId":"public.user"}', 200),
        ),
      );
      final repository = RemoteIdentityReadRepository(
        IdentityRemoteDataSource(client),
        store,
      );

      final identity = await repository.loadPublicAfWalId('public.user');

      expect(identity.afWalId, 'public.user');
      expect(store.readCount, 0);
      client.close();
    });
  });
}

StoredAuthSession _session() => StoredAuthSession(
      accessToken: 'test-access-token',
      refreshToken: 'test-refresh-token',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 10, 2, 12),
    );

class _FakeAuthSessionStore implements AuthSessionStore {
  _FakeAuthSessionStore(this.session);

  StoredAuthSession? session;
  int readCount = 0;

  @override
  Future<StoredAuthSession?> read() async {
    readCount += 1;
    return session;
  }

  @override
  Future<void> save(StoredAuthSession session) async {
    this.session = session;
  }

  @override
  Future<void> clear() async {
    session = null;
  }
}
