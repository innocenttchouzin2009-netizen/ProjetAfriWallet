import 'dart:convert';
import 'dart:math';

import 'secure_storage_adapter.dart';

abstract interface class AuthDeviceIdentityProvider {
  Future<String> getOrCreate();
}

class PersistentAuthDeviceIdentity implements AuthDeviceIdentityProvider {
  PersistentAuthDeviceIdentity(
    this._storage, {
    String Function()? generateId,
  }) : _generateId = generateId ?? _generateSecureDeviceId;

  static const String storageKey = 'afwal.auth.device-id.v1';

  final SecureStorageAdapter _storage;
  final String Function() _generateId;

  String? _cachedDeviceId;
  Future<String>? _resolveInFlight;

  @override
  Future<String> getOrCreate() async {
    final cached = _cachedDeviceId;
    if (cached != null) {
      return cached;
    }

    final inFlight = _resolveInFlight;
    if (inFlight != null) {
      return inFlight;
    }

    final operation = _loadOrCreate();
    _resolveInFlight = operation;

    try {
      final deviceId = await operation;
      _cachedDeviceId = deviceId;
      return deviceId;
    } finally {
      if (identical(_resolveInFlight, operation)) {
        _resolveInFlight = null;
      }
    }
  }

  Future<String> _loadOrCreate() async {
    final stored = (await _storage.read(key: storageKey))?.trim();
    if (stored != null && stored.isNotEmpty) {
      return stored;
    }

    final generated = _generateId().trim();
    if (generated.isEmpty) {
      throw StateError('Auth device identity generator returned an empty value.');
    }

    await _storage.write(key: storageKey, value: generated);
    return generated;
  }

  static String _generateSecureDeviceId() {
    final random = Random.secure();
    final bytes = List<int>.generate(16, (_) => random.nextInt(256));
    final encoded = base64UrlEncode(bytes).replaceAll('=', '');
    return 'afw-device-$encoded';
  }
}
