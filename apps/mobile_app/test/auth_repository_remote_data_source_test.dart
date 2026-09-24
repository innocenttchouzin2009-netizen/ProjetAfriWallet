import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_remote_data_source.dart';
import 'package:mobile_app/services/auth_repository.dart';
import 'package:mobile_app/services/secure_session_store.dart';

void main() {
  group('AuthRemoteDataSource', () {
    test('login posts the exact backend contract', () async {
      late http.Request captured;
      final remote = _remote((request) async {
        captured = request;
        return http.Response(
          jsonEncode(_sessionResponse()),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });

      final result = await remote.login(
        const AuthLoginRequest(
          identifier: 'user@example.com',
          password: 'secret',
          deviceId: 'device-1',
          platform: 'android',
          deviceName: 'Pixel',
        ),
      );

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/auth/login');
      expect(jsonDecode(captured.body), <String, Object?>{
        'identifier': 'user@example.com',
        'password': 'secret',
        'deviceId': 'device-1',
        'platform': 'android',
        'deviceName': 'Pixel',
      });
      expect(result.sessionId, 'session-1');
    });

    test('refresh posts only the refresh token', () async {
      late http.Request captured;
      final remote = _remote((request) async {
        captured = request;
        return http.Response(jsonEncode(_sessionResponse()), 200);
      });

      await remote.refresh(
        const AuthRefreshRequest(refreshToken: 'refresh-old'),
      );

      expect(captured.url.path, '/api/v1/auth/refresh');
      expect(jsonDecode(captured.body), <String, Object?>{
        'refreshToken': 'refresh-old',
      });
    });

    test('protected endpoints send the bearer token', () async {
      final requests = <http.Request>[];
      final remote = _remote((request) async {
        requests.add(request);
        if (request.url.path == '/api/v1/auth/session') {
          return http.Response(
            jsonEncode(<String, Object?>{
              'userId': 'user-1',
              'sessionId': 'session-1',
              'deviceId': 'device-1',
              'createdAtUtc': '2026-09-24T15:00:00Z',
              'expiresAtUtc': '2026-10-24T15:00:00Z',
              'tokenVersion': 1,
            }),
            200,
          );
        }
        return http.Response('', 204);
      });

      await remote.loadCurrentSession('access-secret');
      await remote.logout('access-secret');
      await remote.logoutAll('access-secret');

      expect(requests.map((request) => request.url.path), <String>[
        '/api/v1/auth/session',
        '/api/v1/auth/logout',
        '/api/v1/auth/logout-all',
      ]);
      for (final request in requests) {
        expect(request.headers['Authorization'], 'Bearer access-secret');
      }
    });
  });

  group('RemoteAuthRepository', () {
    test('login persists the returned session', () async {
      final store = _MemorySessionStore();
      final repository = RemoteAuthRepository(
        _remote((request) async {
          return http.Response(jsonEncode(_sessionResponse()), 200);
        }),
        store,
        utcNow: () => DateTime.parse('2026-09-24T16:00:00Z'),
      );

      final session = await repository.login(
        const AuthLoginRequest(
          identifier: 'user@example.com',
          password: 'secret',
          deviceId: 'device-1',
          platform: 'android',
        ),
      );

      expect(store.value, same(session));
      expect(
        session.accessTokenExpiresAtUtc,
        DateTime.parse('2026-09-24T16:15:00Z'),
      );
    });

    test('refresh uses stored refresh token and replaces the session', () async {
      final store = _MemorySessionStore()
        ..value = _stored(refreshToken: 'refresh-old');
      late Object? postedBody;
      final repository = RemoteAuthRepository(
        _remote((request) async {
          postedBody = jsonDecode(request.body);
          return http.Response(
            jsonEncode(_sessionResponse(refreshToken: 'refresh-new')),
            200,
          );
        }),
        store,
        utcNow: () => DateTime.parse('2026-09-24T16:00:00Z'),
      );

      final refreshed = await repository.refresh();

      expect(postedBody, <String, Object?>{'refreshToken': 'refresh-old'});
      expect(refreshed.refreshToken, 'refresh-new');
      expect(store.value?.refreshToken, 'refresh-new');
    });

    test('backend auth error is mapped to the stable auth code', () async {
      final repository = RemoteAuthRepository(
        _remote((request) async {
          return http.Response(
            jsonEncode(<String, Object?>{
              'code': 'AUTH_INVALID_CREDENTIALS',
              'message': 'Invalid credentials.',
              'traceId': 'trace-1',
            }),
            401,
          );
        }),
        _MemorySessionStore(),
      );

      expect(
        () => repository.login(
          const AuthLoginRequest(
            identifier: 'user@example.com',
            password: 'wrong',
            deviceId: 'device-1',
            platform: 'android',
          ),
        ),
        throwsA(
          isA<AuthRepositoryException>().having(
            (error) => error.error.code,
            'code',
            AuthErrorCode.invalidCredentials,
          ),
        ),
      );
    });

    test('logout clears local session even when remote revocation fails', () async {
      final store = _MemorySessionStore()..value = _stored();
      final repository = RemoteAuthRepository(
        _remote((request) async => http.Response('offline', 503)),
        store,
      );

      await expectLater(
        repository.logout(),
        throwsA(isA<AuthRepositoryException>()),
      );
      expect(store.value, isNull);
    });

    test('session-dependent operations fail closed without local session', () async {
      final repository = RemoteAuthRepository(
        _remote((request) async => http.Response('', 500)),
        _MemorySessionStore(),
      );

      await expectLater(
        repository.refresh(),
        throwsA(
          isA<AuthRepositoryException>().having(
            (error) => error.error.code,
            'code',
            AuthErrorCode.sessionExpired,
          ),
        ),
      );
    });
  });
}

AuthRemoteDataSource _remote(
  Future<http.Response> Function(http.Request request) handler,
) {
  final client = MockClient(handler);
  return AuthRemoteDataSource(
    ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: client,
    ),
  );
}

Map<String, Object?> _sessionResponse({
  String refreshToken = 'refresh-secret',
}) {
  return <String, Object?>{
    'accessToken': 'access-secret',
    'refreshToken': refreshToken,
    'tokenType': 'Bearer',
    'expiresIn': 900,
    'sessionId': 'session-1',
    'userId': 'user-1',
  };
}

StoredAuthSession _stored({
  String refreshToken = 'refresh-secret',
}) {
  return StoredAuthSession(
    accessToken: 'access-secret',
    refreshToken: refreshToken,
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.parse('2026-09-24T17:00:00Z'),
  );
}

class _MemorySessionStore implements AuthSessionStore {
  StoredAuthSession? value;

  @override
  Future<void> save(StoredAuthSession session) async {
    value = session;
  }

  @override
  Future<StoredAuthSession?> read() async => value;

  @override
  Future<void> clear() async {
    value = null;
  }
}
