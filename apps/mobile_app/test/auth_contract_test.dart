import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/current_auth_session.dart';

void main() {
  test('login request matches backend LoginRequest contract', () {
    const request = AuthLoginRequest(
      identifier: 'user@example.com',
      password: 'secret',
      deviceId: 'device-123',
      platform: 'android',
      deviceName: 'Pixel',
    );

    expect(request.toJson(), <String, Object?>{
      'identifier': 'user@example.com',
      'password': 'secret',
      'deviceId': 'device-123',
      'platform': 'android',
      'deviceName': 'Pixel',
    });
  });

  test('refresh request matches backend RefreshRequest contract', () {
    const request = AuthRefreshRequest(refreshToken: 'refresh-token');

    expect(request.toJson(), <String, Object?>{
      'refreshToken': 'refresh-token',
    });
  });

  test('parses backend AuthSessionResponse contract', () {
    final session = AuthSession.fromJson(<String, Object?>{
      'accessToken': 'access-token',
      'refreshToken': 'refresh-token',
      'tokenType': 'Bearer',
      'expiresIn': 900,
      'sessionId': '9dbdc9d3-2b4a-44cc-8bc5-a82578c8d724',
      'userId': '72317790-a921-4d0a-94a8-75d7d36c5064',
    });

    expect(session.accessToken, 'access-token');
    expect(session.refreshToken, 'refresh-token');
    expect(session.tokenType, 'Bearer');
    expect(session.expiresIn, 900);
    expect(session.sessionId, '9dbdc9d3-2b4a-44cc-8bc5-a82578c8d724');
    expect(session.userId, '72317790-a921-4d0a-94a8-75d7d36c5064');
  });

  test('parses backend CurrentSessionResponse contract', () {
    final session = CurrentAuthSession.fromJson(<String, Object?>{
      'userId': '72317790-a921-4d0a-94a8-75d7d36c5064',
      'sessionId': '9dbdc9d3-2b4a-44cc-8bc5-a82578c8d724',
      'deviceId': 'device-123',
      'createdAtUtc': '2026-09-09T08:00:00Z',
      'expiresAtUtc': '2026-10-09T08:00:00Z',
      'tokenVersion': 3,
    });

    expect(session.deviceId, 'device-123');
    expect(session.createdAtUtc.isUtc, isTrue);
    expect(session.expiresAtUtc.isUtc, isTrue);
    expect(session.tokenVersion, 3);
  });

  test('rejects malformed auth session contract', () {
    expect(
      () => AuthSession.fromJson(<String, Object?>{
        'accessToken': 'access-token',
        'refreshToken': 'refresh-token',
        'tokenType': 'Bearer',
        'expiresIn': '900',
        'sessionId': 'session-id',
        'userId': 'user-id',
      }),
      throwsFormatException,
    );
  });
}
