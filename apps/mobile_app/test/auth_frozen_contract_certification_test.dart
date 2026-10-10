import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/current_auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_remote_data_source.dart';

const String _mobileFreeze =
    'mobile-beta2-dlv-beta2.13 @ 06bb8fc948f8fc66dc2617733d5405e31df865f1';

const Set<String> _frozenBackendErrorCodes = <String>{
  'AUTH_INVALID_CREDENTIALS',
  'AUTH_IDENTIFIER_ALREADY_EXISTS',
  'AUTH_SESSION_EXPIRED',
  'AUTH_SESSION_REVOKED',
  'AUTH_REFRESH_INVALID',
  'AUTH_REFRESH_EXPIRED',
  'AUTH_REFRESH_REUSED',
  'AUTH_TOKEN_INVALID',
  'AUTH_TOKEN_EXPIRED',
  'AUTH_USER_DISABLED',
  'AUTH_VALIDATION_ERROR',
};

void main() {
  group('Frozen Auth contract matrix — $_mobileFreeze', () {
    test('routes, verbs, bearer usage and request DTOs match the freezes',
        () async {
      final requests = <http.Request>[];
      final remote = _remote((request) async {
        requests.add(request);

        if (request.url.path == '/api/v1/auth/session') {
          return http.Response(
            jsonEncode(<String, Object?>{
              'userId': 'user-1',
              'sessionId': 'session-1',
              'deviceId': 'device-1',
              'createdAtUtc': '2026-09-24T09:00:00Z',
              'expiresAtUtc': '2026-10-24T09:00:00Z',
              'tokenVersion': 3,
            }),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }

        if (request.url.path == '/api/v1/auth/logout' ||
            request.url.path == '/api/v1/auth/logout-all') {
          return http.Response('', 204);
        }

        return http.Response(
          jsonEncode(<String, Object?>{
            'accessToken': 'access',
            'refreshToken': 'refresh-new',
            'tokenType': 'Bearer',
            'expiresIn': 900,
            'sessionId': 'session-1',
            'userId': 'user-1',
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });

      await remote.login(
        const AuthLoginRequest(
          identifier: 'user@example.com',
          password: 'secret',
          deviceId: 'device-1',
          platform: 'android',
          deviceName: 'Pixel',
        ),
      );
      await remote.refresh(
        const AuthRefreshRequest(refreshToken: 'refresh-old'),
      );
      await remote.loadCurrentSession('access-secret');
      await remote.logout('access-secret');
      await remote.logoutAll('access-secret');

      expect(
        requests.map((request) => '${request.method} ${request.url.path}'),
        <String>[
          'POST /api/v1/auth/login',
          'POST /api/v1/auth/refresh',
          'GET /api/v1/auth/session',
          'POST /api/v1/auth/logout',
          'POST /api/v1/auth/logout-all',
        ],
      );

      expect(jsonDecode(requests[0].body), <String, Object?>{
        'identifier': 'user@example.com',
        'password': 'secret',
        'deviceId': 'device-1',
        'platform': 'android',
        'deviceName': 'Pixel',
      });
      expect(jsonDecode(requests[1].body), <String, Object?>{
        'refreshToken': 'refresh-old',
      });

      for (final request in requests.skip(2)) {
        expect(request.headers['Authorization'], 'Bearer access-secret');
      }
    });

    test('response DTO wire shapes match the frozen backend contract', () {
      final authSession = AuthSessionResponse.fromJson(<String, Object?>{
        'accessToken': 'access',
        'refreshToken': 'refresh',
        'tokenType': 'Bearer',
        'expiresIn': 900,
        'sessionId': 'session-1',
        'userId': 'user-1',
      });

      expect(authSession.accessToken, 'access');
      expect(authSession.refreshToken, 'refresh');
      expect(authSession.tokenType, 'Bearer');
      expect(authSession.expiresIn, 900);
      expect(authSession.sessionId, 'session-1');
      expect(authSession.userId, 'user-1');

      final currentSession = CurrentAuthSession.fromJson(<String, Object?>{
        'userId': 'user-1',
        'sessionId': 'session-1',
        'deviceId': 'device-1',
        'createdAtUtc': '2026-09-24T09:00:00Z',
        'expiresAtUtc': '2026-10-24T09:00:00Z',
        'tokenVersion': 3,
      });

      expect(currentSession.userId, 'user-1');
      expect(currentSession.sessionId, 'session-1');
      expect(currentSession.deviceId, 'device-1');
      expect(currentSession.createdAtUtc.isUtc, isTrue);
      expect(currentSession.expiresAtUtc.isUtc, isTrue);
      expect(currentSession.tokenVersion, 3);

      final error = AuthError.fromJson(<String, Object?>{
        'code': 'AUTH_SESSION_REVOKED',
        'message': 'Session revoked.',
        'traceId': 'trace-1',
      });

      expect(error.code, AuthErrorCode.sessionRevoked);
      expect(error.message, 'Session revoked.');
      expect(error.traceId, 'trace-1');
    });

    test('mobile error codes mirror backend freeze plus local unknown fallback',
        () {
      final actual = AuthErrorCode.values
          .map((value) => value.wireValue)
          .where((value) => value != 'AUTH_UNKNOWN')
          .toSet();

      expect(actual, unorderedEquals(_frozenBackendErrorCodes));
      expect(
        AuthErrorCode.values.map((value) => value.wireValue),
        contains('AUTH_UNKNOWN'),
      );

      for (final code in _frozenBackendErrorCodes) {
        expect(
          AuthErrorCode.fromWireValue(code),
          isNot(AuthErrorCode.unknown),
          reason: 'Frozen backend code $code must remain mapped on mobile.',
        );
      }

      expect(
        AuthErrorCode.fromWireValue('AUTH_FUTURE_CODE'),
        AuthErrorCode.unknown,
      );
    });
  });
}

AuthRemoteDataSource _remote(
  Future<http.Response> Function(http.Request request) handler,
) {
  return AuthRemoteDataSource(
    ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(handler),
    ),
  );
}
