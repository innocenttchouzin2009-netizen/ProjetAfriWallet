import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_session_store.dart';
import 'package:mobile_app/services/auth_state_controller.dart';

void main() {
  test('auth state starts in restoring state without a session', () {
    final controller = AuthStateController(
      SecureSessionStore(_FakeSecureStorage()),
    );

    expect(controller.state.status, AuthStateStatus.restoring);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
  });

  test('restore exposes a valid persisted session as authenticated', () async {
    final storage = _FakeSecureStorage();
    final now = DateTime.utc(2026, 9, 25, 18);
    final store = SecureSessionStore(storage, utcNow: () => now);

    await store.save(
      const AuthSessionResponse(
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        tokenType: 'Bearer',
        expiresIn: 900,
        sessionId: 'session-1',
        userId: 'user-1',
      ),
    );

    final controller = AuthStateController(store);
    await controller.restore();

    expect(controller.state.status, AuthStateStatus.authenticated);
    expect(controller.state.isAuthenticated, isTrue);
    expect(controller.state.session?.sessionId, 'session-1');
    expect(controller.state.session?.userId, 'user-1');
  });

  test('restore resolves to unauthenticated when no session exists', () async {
    final controller = AuthStateController(
      SecureSessionStore(_FakeSecureStorage()),
    );

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.session, isNull);
  });

  test('restore stays local and rejects an expired stored session', () async {
    final storage = _FakeSecureStorage();
    var now = DateTime.utc(2026, 9, 25, 18);
    final store = SecureSessionStore(storage, utcNow: () => now);

    await store.save(
      const AuthSessionResponse(
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        tokenType: 'Bearer',
        expiresIn: 60,
        sessionId: 'session-1',
        userId: 'user-1',
      ),
    );

    now = now.add(const Duration(seconds: 60));

    final controller = AuthStateController(store);
    await controller.restore();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.session, isNull);
    expect(storage.values, isEmpty);
  });

  test('restore fails closed when secure storage cannot be read', () async {
    final controller = AuthStateController(
      SecureSessionStore(_ThrowingSecureStorage()),
    );

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.restorationFailed);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
  });

  test('clearLocalSession clears storage and local auth state', () async {
    final storage = _FakeSecureStorage();
    final now = DateTime.utc(2026, 9, 25, 18);
    final store = SecureSessionStore(storage, utcNow: () => now);

    await store.save(
      const AuthSessionResponse(
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        tokenType: 'Bearer',
        expiresIn: 900,
        sessionId: 'session-1',
        userId: 'user-1',
      ),
    );

    final controller = AuthStateController(store);
    await controller.restore();
    await controller.clearLocalSession();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(storage.values, isEmpty);
  });
}

class _FakeSecureStorage implements SecureKeyValueStorage {
  final Map<String, String> values = <String, String>{};

  @override
  Future<void> delete(String key) async {
    values.remove(key);
  }

  @override
  Future<String?> read(String key) async => values[key];

  @override
  Future<void> write(String key, String value) async {
    values[key] = value;
  }
}

class _ThrowingSecureStorage implements SecureKeyValueStorage {
  @override
  Future<void> delete(String key) async {}

  @override
  Future<String?> read(String key) async {
    throw StateError('secure storage unavailable');
  }

  @override
  Future<void> write(String key, String value) async {}
}
