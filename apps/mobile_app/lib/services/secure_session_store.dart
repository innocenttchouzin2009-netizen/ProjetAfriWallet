import 'dart:convert';

import '../models/auth_session.dart';
import 'secure_storage_adapter.dart';

abstract interface class AuthSessionStore {
  Future<void> save(StoredAuthSession session);
  Future<StoredAuthSession?> read();
  Future<void> clear();
}

class SecureSessionStore implements AuthSessionStore {
  SecureSessionStore(this._storage);

  static const String _sessionKey = 'afwal.auth.session.v1';

  final SecureStorageAdapter _storage;

  @override
  Future<void> save(StoredAuthSession session) {
    final payload = jsonEncode(session.toJson());
    return _storage.write(key: _sessionKey, value: payload);
  }

  @override
  Future<StoredAuthSession?> read() async {
    final payload = await _storage.read(key: _sessionKey);
    if (payload == null || payload.isEmpty) {
      return null;
    }

    try {
      final decoded = jsonDecode(payload);
      if (decoded is! Map<String, dynamic>) {
        throw const FormatException('Invalid stored auth session payload.');
      }
      return StoredAuthSession.fromJson(decoded.cast<String, Object?>());
    } on FormatException {
      await clear();
      return null;
    }
  }

  @override
  Future<void> clear() => _storage.delete(key: _sessionKey);
}
