import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_session_bootstrap.dart';
import 'package:mobile_app/services/auth_state_controller.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  test('bootstrap restores no session as unauthenticated', () async {
    final bootstrap = await AuthSessionBootstrap.create(
      storage: _MemorySecureStorage(),
    );
    addTearDown(bootstrap.dispose);

    expect(
      bootstrap.controller.state.status,
      AuthStateStatus.unauthenticated,
    );
    expect(bootstrap.controller.state.session, isNull);
  });

  test('bootstrap restores a persisted secure session', () async {
    final storage = _MemorySecureStorage();
    final store = SecureSessionStore(storage);
    await store.save(_session());

    final bootstrap = await AuthSessionBootstrap.create(storage: storage);
    addTearDown(bootstrap.dispose);

    expect(
      bootstrap.controller.state.status,
      AuthStateStatus.authenticated,
    );
    expect(bootstrap.controller.state.session?.accessToken, 'access-token');
    expect(bootstrap.controller.state.session?.refreshToken, 'refresh-token');
  });

  test('bootstrap fails closed when secure storage cannot be read', () async {
    final bootstrap = await AuthSessionBootstrap.create(
      storage: _ThrowingSecureStorage(),
    );
    addTearDown(bootstrap.dispose);

    expect(
      bootstrap.controller.state.status,
      AuthStateStatus.restorationFailed,
    );
    expect(bootstrap.controller.state.session, isNull);
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

class _MemorySecureStorage implements SecureStorageAdapter {
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
  Future<String?> read({required String key}) {
    throw StateError('secure storage unavailable');
  }

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {}
}
