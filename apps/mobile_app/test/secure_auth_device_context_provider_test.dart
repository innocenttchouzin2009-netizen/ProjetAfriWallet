import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/services/secure_auth_device_context_provider.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  group('SecureAuthDeviceContextProvider', () {
    late _FakeSecureStorageAdapter storage;

    setUp(() {
      storage = _FakeSecureStorageAdapter();
    });

    test('creates and securely persists a stable device id', () async {
      var generated = 0;
      final provider = SecureAuthDeviceContextProvider(
        storage,
        platformResolver: () => 'android',
        deviceIdFactory: () {
          generated += 1;
          return 'device-generated';
        },
      );

      final first = await provider.load();
      final second = await provider.load();

      expect(first.deviceId, 'device-generated');
      expect(second.deviceId, 'device-generated');
      expect(generated, 1);
      expect(
        storage.values[SecureAuthDeviceContextProvider.storageKey],
        'device-generated',
      );
    });

    test('reuses an existing secure device id without regenerating it', () async {
      storage.values[SecureAuthDeviceContextProvider.storageKey] =
          'device-existing';

      final provider = SecureAuthDeviceContextProvider(
        storage,
        platformResolver: () => 'ios',
        deviceIdFactory: () => throw StateError('must not generate'),
      );

      final context = await provider.load();

      expect(context.deviceId, 'device-existing');
      expect(context.platform, 'ios');
    });

    test('replaces an empty stored device id', () async {
      storage.values[SecureAuthDeviceContextProvider.storageKey] = '   ';

      final provider = SecureAuthDeviceContextProvider(
        storage,
        platformResolver: () => 'android',
        deviceIdFactory: () => 'device-replacement',
      );

      final context = await provider.load();

      expect(context.deviceId, 'device-replacement');
      expect(
        storage.values[SecureAuthDeviceContextProvider.storageKey],
        'device-replacement',
      );
    });

    test('returns platform and optional device name without persisting them',
        () async {
      final provider = SecureAuthDeviceContextProvider(
        storage,
        platformResolver: () => 'android',
        deviceNameResolver: () => 'Test Device',
        deviceIdFactory: () => 'device-1',
      );

      final context = await provider.load();

      expect(context.platform, 'android');
      expect(context.deviceName, 'Test Device');
      expect(storage.values.length, 1);
    });
  });
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
