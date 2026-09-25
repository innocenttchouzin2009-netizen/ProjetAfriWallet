import 'dart:convert';

import '../models/auth_session.dart';

abstract interface class SecureKeyValueStorage {
  Future<String?> read(String key);
  Future<void> write(String key, String value);
  Future<void> delete(String key);
}

class SecureSessionStore {
  SecureSessionStore(
    this._storage, {
    DateTime Function()? utcNow,
  }) : _utcNow = utcNow ?? DateTime.now;

  static const storageKey = 'afw.auth.session.v1';

  final SecureKeyValueStorage _storage;
  final DateTime Function() _utcNow;

  Future<void> save(AuthSessionResponse session) async {
    final stored = session.toStored(receivedAtUtc: _utcNow().toUtc());
    await _storage.write(storageKey, jsonEncode(stored.toJson()));
  }

  Future<StoredAuthSession?> read() async {
    final raw = await _storage.read(storageKey);
    if (raw == null) {
      return null;
    }

    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map<String, dynamic>) {
        await clear();
        return null;
      }

      final session = StoredAuthSession.fromJson(
        Map<String, Object?>.from(decoded),
      );

      if (session.isAccessTokenExpired(_utcNow())) {
        await clear();
        return null;
      }

      return session;
    } on FormatException {
      await clear();
      return null;
    }
  }

  Future<void> clear() => _storage.delete(storageKey);
}
