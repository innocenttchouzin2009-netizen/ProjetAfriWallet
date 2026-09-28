import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  group('canonical auth session store', () {
    test('persists one complete session payload under the canonical key', () async {
      final storage = _FakeSecureStorage();
      final store = SecureSessionStore(storage);

      await store.save(_session());

      expect(storage.values.keys, <String>[SecureSessionStore.storageKey]);

      final raw = storage.values[SecureSessionStore.storageKey]!;
      final persisted = jsonDecode(raw) as Map<String, dynamic>;
      expect(persisted['accessToken'], 'access-token');
      expect(persisted['refreshToken'], 'refresh-token');
      expect(
        persisted['accessTokenExpiresAtUtc'],
        '2026-09-25T18:15:00.000Z',
      );
    });

    test('keeps refresh material when only the access token is expired', () async {
      final storage = _FakeSecureStorage();
      final store = SecureSessionStore(storage);
      await store.save(
        _session(
          accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 25, 18),
        ),
      );

      final restored = await store.read();

      expect(restored, isNotNull);
      expect(restored!.refreshToken, 'refresh-token');
      expect(storage.values, isNotEmpty);
    });

    test('clears malformed persisted data', () async {
      final storage = _FakeSecureStorage()
        ..values[SecureSessionStore.storageKey] = '{"accessToken":42}';
      final store = SecureSessionStore(storage);

      expect(await store.read(), isNull);
      expect(storage.values, isEmpty);
    });

    test('clear removes the persisted session payload', () async {
      final storage = _FakeSecureStorage()
        ..values[SecureSessionStore.storageKey] = '{}';
      final store = SecureSessionStore(storage);

      await store.clear();

      expect(storage.values, isEmpty);
    });
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
