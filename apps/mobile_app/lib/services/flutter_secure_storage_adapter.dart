import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'secure_storage_adapter.dart';

class FlutterSecureStorageAdapter implements SecureStorageAdapter {
  FlutterSecureStorageAdapter({
    FlutterSecureStorage? storage,
  }) : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  @override
  Future<String?> read({required String key}) {
    return _storage.read(key: key);
  }

  @override
  Future<void> write({
    required String key,
    required String value,
  }) {
    return _storage.write(key: key, value: value);
  }

  @override
  Future<void> delete({required String key}) {
    return _storage.delete(key: key);
  }
}
