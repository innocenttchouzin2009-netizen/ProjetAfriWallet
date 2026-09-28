import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_state_controller.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  test('auth state starts in restoring state without a session', () {
    final controller = AuthStateController(
      SecureSessionStore(_FakeSecureStorageAdapter()),
    );

    expect(controller.state.status, AuthStateStatus.restoring);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
  });

  test('restore exposes an active persisted session as authenticated', () async {
    final storage = _FakeSecureStorageAdapter();
    final store = SecureSessionStore(storage);
    await store.save(_session(DateTime.utc(2026, 9, 28, 10, 15)));

    final controller = AuthStateController(
      store,
      utcNow: () => DateTime.utc(2026, 9, 28, 10),
    );
    await controller.restore();

    expect(controller.state.status, AuthStateStatus.authenticated);
    expect(controller.state.isAuthenticated, isTrue);
    expect(controller.state.hasStoredSession, isTrue);
    expect(controller.state.session?.sessionId, 'session-1');
  });

  test('restore resolves to unauthenticated when no session exists', () async {
    final controller = AuthStateController(
      SecureSessionStore(_FakeSecureStorageAdapter()),
      utcNow: () => DateTime.utc(2026, 9, 28, 10),
    );

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.session, isNull);
    expect(controller.state.hasStoredSession, isFalse);
  });

  test('restore marks expired access token as refresh required locally', () async {
    final storage = _FakeSecureStorageAdapter();
    final store = SecureSessionStore(storage);
    await store.save(_session(DateTime.utc(2026, 9, 28, 10)));

    final controller = AuthStateController(
      store,
      utcNow: () => DateTime.utc(2026, 9, 28, 10),
    );
    await controller.restore();

    expect(controller.state.status, AuthStateStatus.refreshRequired);
    expect(controller.state.isAuthenticated, isFalse);
    expect(controller.state.hasStoredSession, isTrue);
    expect(controller.state.session?.refreshToken, 'refresh-token');
    expect(storage.values, isNotEmpty);
  });

  test('restore fails closed when secure storage cannot be read', () async {
    final controller = AuthStateController(
      SecureSessionStore(_ThrowingSecureStorageAdapter()),
    );

    await controller.restore();

    expect(controller.state.status, AuthStateStatus.restorationFailed);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
  });

  test('clearLocalSession clears storage and local auth state', () async {
    final storage = _FakeSecureStorageAdapter();
    final store = SecureSessionStore(storage);
    await store.save(_session(DateTime.utc(2026, 9, 28, 10, 15)));

    final controller = AuthStateController(
      store,
      utcNow: () => DateTime.utc(2026, 9, 28, 10),
    );
    await controller.restore();
    await controller.clearLocalSession();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(controller.state.hasStoredSession, isFalse);
    expect(storage.values, isEmpty);
  });
}

StoredAuthSession _session(DateTime expiresAtUtc) {
  return StoredAuthSession(
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: expiresAtUtc,
  );
}

class _FakeSecureStorageAdapter implements SecureStorageAdapter {
  final Map<String, String> values = <String, String>{};

  @override
  Future<void> delete({required String key}) async {
    values.remove(key);
  }

  @override
  Future<String?> read({required String key}) async => values[key];

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {
    values[key] = value;
  }
}

class _ThrowingSecureStorageAdapter implements SecureStorageAdapter {
  @override
  Future<void> delete({required String key}) async {}

  @override
  Future<String?> read({required String key}) async {
    throw StateError('secure storage unavailable');
  }

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {}
}
