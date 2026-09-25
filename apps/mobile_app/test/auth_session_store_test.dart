import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_session_store.dart';

void main() {
  test('secure session store persists a single session payload', () async {
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

    expect(storage.values.keys, <String>[SecureSessionStore.storageKey]);

    final raw = storage.values[SecureSessionStore.storageKey]!;
    final persisted = jsonDecode(raw) as Map<String, dynamic>;
    expect(
      persisted['accessTokenExpiresAtUtc'],
      '2026-09-25T18:15:00.000Z',
    );

    final restored = await store.read();
    expect(restored, isNotNull);
    expect(restored!.accessToken, 'access-token');
    expect(restored.refreshToken, 'refresh-token');
    expect(restored.sessionId, 'session-1');
  });

  test('secure session store clears a session at access-token expiry', () async {
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

    expect(await store.read(), isNull);
    expect(storage.values, isEmpty);
  });

  test('secure session store clears malformed persisted data', () async {
    final storage = _FakeSecureStorage()
      ..values[SecureSessionStore.storageKey] = '{"accessToken":42}';
    final store = SecureSessionStore(
      storage,
      utcNow: () => DateTime.utc(2026, 9, 25, 18),
    );

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
