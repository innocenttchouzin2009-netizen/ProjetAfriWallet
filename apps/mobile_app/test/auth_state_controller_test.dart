import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_repository.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/auth_state_controller.dart';

void main() {
  test('auth state starts in restoring state without a session', () {
    final controller = AuthStateController(_FakeSessionLifecycle());

    expect(controller.state.status, AuthStateStatus.restoring);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
    expect(controller.state.loginStatus, AuthLoginStatus.idle);
    expect(controller.state.loginError, isNull);
  });

  test('login exposes authenticating state then authenticated session', () async {
    final completer = Completer<StoredAuthSession>();
    final lifecycle = _FakeSessionLifecycle(
      loginHandler: (_) => completer.future,
    );
    final controller = AuthStateController(lifecycle);

    final login = controller.login(_loginRequest());
    await Future<void>.delayed(Duration.zero);

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.loginStatus, AuthLoginStatus.authenticating);
    expect(controller.state.isAuthenticating, isTrue);
    expect(controller.state.loginError, isNull);
    expect(lifecycle.loginCalls, 1);
    expect(lifecycle.lastLoginRequest?.identifier, 'user@example.com');

    completer.complete(_session());
    await login;

    expect(controller.state.status, AuthStateStatus.authenticated);
    expect(controller.state.isAuthenticated, isTrue);
    expect(controller.state.loginStatus, AuthLoginStatus.idle);
    expect(controller.state.session?.sessionId, 'session-1');
  });

  test('login exposes stable repository error without authenticating', () async {
    final lifecycle = _FakeSessionLifecycle(
      loginHandler: (_) async {
        throw const AuthRepositoryException(
          AuthError(
            code: AuthErrorCode.invalidCredentials,
            message: 'Invalid credentials.',
          ),
        );
      },
    );
    final controller = AuthStateController(lifecycle);

    await controller.login(_loginRequest());

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.isAuthenticated, isFalse);
    expect(controller.state.loginStatus, AuthLoginStatus.failed);
    expect(
      controller.state.loginError?.code,
      AuthErrorCode.invalidCredentials,
    );
    expect(controller.state.loginError?.message, 'Invalid credentials.');
  });

  test('login fails closed with unknown error for unexpected failures', () async {
    final lifecycle = _FakeSessionLifecycle(
      loginHandler: (_) async => throw StateError('unexpected'),
    );
    final controller = AuthStateController(lifecycle);

    await controller.login(_loginRequest());

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.loginStatus, AuthLoginStatus.failed);
    expect(controller.state.loginError?.code, AuthErrorCode.unknown);
    expect(controller.state.isAuthenticated, isFalse);
  });

  test('restore exposes a resolved session as authenticated', () async {
    final controller = AuthStateController(
      _FakeSessionLifecycle(session: _session()),
    );

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.authenticated);
    expect(controller.state.isAuthenticated, isTrue);
    expect(controller.state.session?.sessionId, 'session-1');
    expect(controller.state.session?.userId, 'user-1');
    expect(controller.state.loginStatus, AuthLoginStatus.idle);
  });

  test('restore resolves to unauthenticated when lifecycle returns no session',
      () async {
    final controller = AuthStateController(_FakeSessionLifecycle());

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.session, isNull);
    expect(controller.state.loginStatus, AuthLoginStatus.idle);
  });

  test('restore fails closed when lifecycle restoration throws', () async {
    final controller = AuthStateController(
      _FakeSessionLifecycle(restoreError: StateError('refresh unavailable')),
    );

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.restorationFailed);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
    expect(controller.state.loginStatus, AuthLoginStatus.idle);
  });

  test('clearLocalSession invalidates lifecycle and local auth state', () async {
    final lifecycle = _FakeSessionLifecycle(session: _session());
    final controller = AuthStateController(lifecycle);

    await controller.restore();
    await controller.clearLocalSession();

    expect(lifecycle.clearCalls, 1);
    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.session, isNull);
    expect(controller.state.loginStatus, AuthLoginStatus.idle);
  });
}

AuthLoginRequest _loginRequest() {
  return const AuthLoginRequest(
    identifier: 'user@example.com',
    password: 'correct-horse-battery-staple',
    deviceId: 'device-1',
    platform: 'android',
    deviceName: 'Pixel',
  );
}

StoredAuthSession _session() {
  return StoredAuthSession(
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 29, 20),
  );
}

class _FakeSessionLifecycle implements AuthSessionLifecycle {
  _FakeSessionLifecycle({
    this.session,
    this.restoreError,
    this.loginHandler,
  });

  StoredAuthSession? session;
  final Object? restoreError;
  final Future<StoredAuthSession> Function(AuthLoginRequest request)?
      loginHandler;
  int clearCalls = 0;
  int loginCalls = 0;
  AuthLoginRequest? lastLoginRequest;

  @override
  Future<StoredAuthSession> login(AuthLoginRequest request) {
    loginCalls += 1;
    lastLoginRequest = request;
    final handler = loginHandler;
    if (handler == null) {
      throw StateError('Unexpected login call.');
    }
    return handler(request);
  }

  @override
  Future<StoredAuthSession?> restoreValidSession() async {
    final error = restoreError;
    if (error != null) {
      throw error;
    }
    return session;
  }

  @override
  Future<StoredAuthSession?> refreshSession() async => session;

  @override
  Future<void> clearLocalSession() async {
    clearCalls += 1;
    session = null;
  }
}
