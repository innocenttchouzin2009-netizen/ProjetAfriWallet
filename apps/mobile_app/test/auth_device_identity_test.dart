import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/services/auth_device_identity.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  group('PersistentAuthDeviceIdentity', () {
    test('returns an existing persisted device identity without rewriting it',
        () async {
      final storage = _MemorySecureStorageAdapter()
        ..values[PersistentAuthDeviceIdentity.storageKey] = 'device-existing';
      var generations = 0;
      final identity = PersistentAuthDeviceIdentity(
        storage,
        generateId: () {
          generations += 1;
          return 'device-generated';
        },
      );

      expect(await identity.getOrCreate(), 'device-existing');
      expect(await identity.getOrCreate(), 'device-existing');
      expect(generations, 0);
      expect(storage.writeCount, 0);
    });

    test('creates and persists a device identity when none exists', () async {
      final storage = _MemorySecureStorageAdapter();
      final identity = PersistentAuthDeviceIdentity(
        storage,
        generateId: () => 'device-generated',
      );

      final deviceId = await identity.getOrCreate();

      expect(deviceId, 'device-generated');
      expect(
        storage.values[PersistentAuthDeviceIdentity.storageKey],
        'device-generated',
      );
      expect(storage.writeCount, 1);
    });

    test('a new provider instance restores the same persisted identity',
        () async {
      final storage = _MemorySecureStorageAdapter();
      final first = PersistentAuthDeviceIdentity(
        storage,
        generateId: () => 'device-first',
      );

      expect(await first.getOrCreate(), 'device-first');

      final second = PersistentAuthDeviceIdentity(
        storage,
        generateId: () => 'device-second',
      );

      expect(await second.getOrCreate(), 'device-first');
      expect(storage.writeCount, 1);
    });

    test('blank stored values are replaced with a generated identity', () async {
      final storage = _MemorySecureStorageAdapter()
        ..values[PersistentAuthDeviceIdentity.storageKey] = '   ';
      final identity = PersistentAuthDeviceIdentity(
        storage,
        generateId: () => 'device-replacement',
      );

      expect(await identity.getOrCreate(), 'device-replacement');
      expect(
        storage.values[PersistentAuthDeviceIdentity.storageKey],
        'device-replacement',
      );
    });

    test('concurrent callers share one creation operation', () async {
      final storage = _BlockingSecureStorageAdapter();
      var generations = 0;
      final identity = PersistentAuthDeviceIdentity(
        storage,
        generateId: () {
          generations += 1;
          return 'device-concurrent';
        },
      );

      final first = identity.getOrCreate();
      final second = identity.getOrCreate();

      storage.releaseRead();

      expect(await first, 'device-concurrent');
      expect(await second, 'device-concurrent');
      expect(generations, 1);
      expect(storage.writeCount, 1);
    });

    test('rejects an empty generated identity instead of persisting it',
        () async {
      final storage = _MemorySecureStorageAdapter();
      final identity = PersistentAuthDeviceIdentity(
        storage,
        generateId: () => '   ',
      );

      await expectLater(identity.getOrCreate(), throwsStateError);
      expect(storage.values, isEmpty);
      expect(storage.writeCount, 0);
    });
  });
}

class _MemorySecureStorageAdapter implements SecureStorageAdapter {
  final Map<String, String> values = <String, String>{};
  int writeCount = 0;

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {
    writeCount += 1;
    values[key] = value;
  }

  @override
  Future<String?> read({required String key}) async => values[key];

  @override
  Future<void> delete({required String key}) async {
    values.remove(key);
  }
}

class _BlockingSecureStorageAdapter extends _MemorySecureStorageAdapter {
  final Completer<void> _readGate = Completer<void>();

  void releaseRead() {
    if (!_readGate.isCompleted) {
      _readGate.complete();
    }
  }

  @override
  Future<String?> read({required String key}) async {
    await _readGate.future;
    return super.read(key: key);
  }
}
