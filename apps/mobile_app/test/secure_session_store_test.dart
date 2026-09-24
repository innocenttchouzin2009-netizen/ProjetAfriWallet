import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  group('SecureSessionStore', () {
    late _FakeSecureStorageAdapter storage;
    late SecureSessionStore store;

    setUp(() {
      storage = _FakeSecureStorageAdapter();
      store = SecureSessionStore(storage);
    });

    test('round-trips the complete auth session as one secure record', () async {
      final session = _session();

      await store.save(session);
      final restored = await store.read();

      expect(restored, isNotNull);
      expect(restored!.accessToken, session.accessToken);
      expect(restored.refreshToken, session.refreshToken);
      expect(restored.sessionId, session.sessionId);
      expect(
        restored.accessTokenExpiresAtUtc,
        session.accessTokenExpiresAtUtc,
      );
      expect(storage.values.length, 1);
    });

    test('clear removes the persisted session', () async {
      await store.save(_session());

      await store.clear();

      expect(await store.read(), isNull);
      expect(storage.values, isEmpty);
    });

    test('missing session returns null', () async {
      expect(await store.read(), isNull);
    });

    test('corrupted session fails closed and deletes the record', () async {
      storage.values['afwal.auth.session.v1'] = '{not-json';

      expect(await store.read(), isNull);
      expect(storage.values, isEmpty);
    });

    test('invalid session shape fails closed and deletes the record', () async {
      storage.values['afwal.auth.session.v1'] = '{"accessToken":"only"}';

      expect(await store.read(), isNull);
      expect(storage.values, isEmpty);
    });
  });
}

StoredAuthSession _session() {
  return StoredAuthSession(
    accessToken: 'access-secret',
    refreshToken: 'refresh-secret',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.parse('2026-09-24T11:00:00Z'),
  );
}

class _FakeSecureStorageAdapter implements SecureStorageAdapter {
  final Map<String, String> values = <String, String>{};

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {
    values[key] = value;
  }

  @override
  Future<String?> read({required String key}) async => values[key];

  @override
  Future<void> delete({required String key}) async {
    values.remove(key);
  }
}
