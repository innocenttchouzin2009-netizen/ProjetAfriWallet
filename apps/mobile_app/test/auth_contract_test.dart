import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/current_auth_session.dart';

void main() {
  group('Auth contracts', () {
    test('login request serializes the backend contract exactly', () {
      const request = AuthLoginRequest(
        identifier: 'user@example.com',
        password: 'secret',
        deviceId: 'device-1',
        platform: 'android',
        deviceName: 'Pixel',
      );

      expect(request.toJson(), <String, Object?>{
        'identifier': 'user@example.com',
        'password': 'secret',
        'deviceId': 'device-1',
        'platform': 'android',
        'deviceName': 'Pixel',
      });
    });

    test('auth session converts relative expiry to an absolute UTC expiry', () {
      final response = AuthSessionResponse.fromJson(<String, Object?>{
        'accessToken': 'access',
        'refreshToken': 'refresh',
        'tokenType': 'Bearer',
        'expiresIn': 900,
        'sessionId': 'session-1',
        'userId': 'user-1',
      });

      final stored = response.toStored(
        receivedAtUtc: DateTime.parse('2026-09-24T10:00:00Z'),
      );

      expect(
        stored.accessTokenExpiresAtUtc,
        DateTime.parse('2026-09-24T10:15:00Z'),
      );
    });

    test('stored session treats the exact expiry instant as expired', () {
      final session = StoredAuthSession(
        accessToken: 'access',
        refreshToken: 'refresh',
        tokenType: 'Bearer',
        sessionId: 'session-1',
        userId: 'user-1',
        accessTokenExpiresAtUtc: DateTime.parse('2026-09-24T10:15:00Z'),
      );

      expect(
        session.isAccessTokenExpired(
          DateTime.parse('2026-09-24T10:15:00Z'),
        ),
        isTrue,
      );
    });

    test('current session parses UTC metadata', () {
      final session = CurrentAuthSession.fromJson(<String, Object?>{
        'userId': 'user-1',
        'sessionId': 'session-1',
        'deviceId': 'device-1',
        'createdAtUtc': '2026-09-24T09:00:00Z',
        'expiresAtUtc': '2026-10-24T09:00:00Z',
        'tokenVersion': 3,
      });

      expect(session.createdAtUtc.isUtc, isTrue);
      expect(session.tokenVersion, 3);
    });

    test('unknown backend auth errors remain stable and non-throwing', () {
      final error = AuthError.fromJson(<String, Object?>{
        'code': 'AUTH_FUTURE_CODE',
        'message': 'Future backend error.',
      });

      expect(error.code, AuthErrorCode.unknown);
    });
  });
}
