import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/auth_state_controller.dart';

void main() {
  test('auth state starts in restoring state without a session', () {
    final controller = AuthStateController(_FakeSessionLifecycle());

    expect(controller.state.status, AuthStateStatus.restoring);
    expect(controller.state.session, isNull);
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
  });

  test('restore resolves to unauthenticated when lifecycle returns no session',
      () async {
    final controller = AuthStateController(_FakeSessionLifecycle());

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.session, isNull);
  });

  test('restore fails closed when lifecycle restoration throws', () async {
    final controller = AuthStateController(
      _FakeSessionLifecycle(restoreError: StateError('refresh unavailable')),
    );

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.restorationFailed);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
  });

  test('clearLocalSession invalidates lifecycle and local auth state', () async {
    final lifecycle = _FakeSessionLifecycle(session: _session());
    final controller = AuthStateController(lifecycle);

    await controller.restore();
    await controller.clearLocalSession();

    expect(lifecycle.clearCalls, 1);
    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.session, isNull);
  });
}

StoredAuthSession _session() {
  return StoredAuthSession(
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 28, 12),
  );
}

class _FakeSessionLifecycle implements AuthSessionLifecycle {
  _FakeSessionLifecycle({
    this.session,
    this.restoreError,
  });

  StoredAuthSession? session;
  final Object? restoreError;
  int clearCalls = 0;

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
