import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_state_controller.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  test('auth state starts in restoring state without a session', () {
    final controller = AuthStateController(
      SecureSessionStore(_FakeSecureStorage()),
    );

    expect(controller.state.status, AuthStateStatus.restoring);
    expect(controller.state.session, isNull);
    expect(controller.state.isAuthenticated, isFalse);
  });

  test('restore exposes a persisted session as authenticated', () async {
    final storage = _FakeSecureStorage();
    final store = SecureSessionStore(storage);
    await store.save(_session());

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

  test('restore preserves refreshable session after access-token expiry', () async {
    final storage = _FakeSecureStorage();
    final store = SecureSessionStore(storage);
    await store.save(
      _session(
        accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 25, 18),
      ),
    );

    final controller = AuthStateController(store);
    await controller.restore();

    expect(controller.state.status, AuthStateStatus.authenticated);
    expect(controller.state.session?.refreshToken, 'refresh-token');
    expect(storage.values, isNotEmpty);
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
    final store = SecureSessionStore(storage);
    await store.save(_session());

    final controller = AuthStateController(store);
    await controller.restore();
    await controller.clearLocalSession();

    expect(controller.state.status, AuthStateStatus.unauthenticated);
    expect(storage.values, isEmpty);
  });
}

StoredAuthSession _session({
  DateTime? accessTokenExpiresAtUtc,
}) {
  return StoredAuthSession(
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc:
        accessTokenExpiresAtUtc ?? DateTime.utc(2026, 9, 25, 18, 15),
  );
}

class _FakeSecureStorage implements SecureStorageAdapter {
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

class _ThrowingSecureStorage implements SecureStorageAdapter {
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
