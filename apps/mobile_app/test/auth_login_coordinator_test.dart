import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/current_auth_session.dart';
import 'package:mobile_app/services/auth_device_identity.dart';
import 'package:mobile_app/services/auth_login_coordinator.dart';
import 'package:mobile_app/services/auth_repository.dart';

void main() {
  group('AuthLoginCoordinator', () {
    test('adds persistent device identity to the login request', () async {
      final repository = _RecordingAuthRepository();
      final deviceIdentity = _FakeAuthDeviceIdentity('device-stable');
      final coordinator = AuthLoginCoordinator(repository, deviceIdentity);

      final session = await coordinator.login(
        identifier: 'user@example.com',
        password: 'secret',
        platform: 'android',
        deviceName: 'Pixel',
      );

      expect(session.sessionId, 'session-1');
      expect(deviceIdentity.calls, 1);
      expect(repository.loginCalls, 1);

      final request = repository.lastLoginRequest;
      expect(request, isNotNull);
      expect(request!.identifier, 'user@example.com');
      expect(request.password, 'secret');
      expect(request.deviceId, 'device-stable');
      expect(request.platform, 'android');
      expect(request.deviceName, 'Pixel');
    });

    test('resolves device identity for each login attempt', () async {
      final repository = _RecordingAuthRepository();
      final deviceIdentity = _FakeAuthDeviceIdentity('device-stable');
      final coordinator = AuthLoginCoordinator(repository, deviceIdentity);

      await coordinator.login(
        identifier: 'first@example.com',
        password: 'one',
        platform: 'ios',
      );
      await coordinator.login(
        identifier: 'second@example.com',
        password: 'two',
        platform: 'ios',
      );

      expect(deviceIdentity.calls, 2);
      expect(repository.loginCalls, 2);
      expect(repository.lastLoginRequest!.deviceId, 'device-stable');
      expect(repository.lastLoginRequest!.deviceName, isNull);
    });

    test('does not call repository when device identity resolution fails',
        () async {
      final repository = _RecordingAuthRepository();
      final coordinator = AuthLoginCoordinator(
        repository,
        _FailingAuthDeviceIdentity(),
      );

      await expectLater(
        coordinator.login(
          identifier: 'user@example.com',
          password: 'secret',
          platform: 'android',
        ),
        throwsStateError,
      );

      expect(repository.loginCalls, 0);
      expect(repository.lastLoginRequest, isNull);
    });
  });
}

class _FakeAuthDeviceIdentity implements AuthDeviceIdentityProvider {
  _FakeAuthDeviceIdentity(this.deviceId);

  final String deviceId;
  int calls = 0;

  @override
  Future<String> getOrCreate() async {
    calls += 1;
    return deviceId;
  }
}

class _FailingAuthDeviceIdentity implements AuthDeviceIdentityProvider {
  @override
  Future<String> getOrCreate() async {
    throw StateError('device identity unavailable');
  }
}

class _RecordingAuthRepository implements AuthRepository {
  int loginCalls = 0;
  AuthLoginRequest? lastLoginRequest;

  @override
  Future<StoredAuthSession> login(AuthLoginRequest request) async {
    loginCalls += 1;
    lastLoginRequest = request;
    return StoredAuthSession(
      accessToken: 'access',
      refreshToken: 'refresh',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2030, 1, 1),
    );
  }

  @override
  Future<StoredAuthSession> refresh() => throw UnimplementedError();

  @override
  Future<StoredAuthSession?> readStoredSession() =>
      throw UnimplementedError();

  @override
  Future<CurrentAuthSession> loadCurrentSession() =>
      throw UnimplementedError();

  @override
  Future<void> logout() => throw UnimplementedError();

  @override
  Future<void> logoutAll() => throw UnimplementedError();
}
