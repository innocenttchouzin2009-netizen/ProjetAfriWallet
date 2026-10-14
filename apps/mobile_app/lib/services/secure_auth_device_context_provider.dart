import 'dart:io';
import 'dart:math';

import '../models/auth_device_context.dart';
import 'auth_device_context_provider.dart';
import 'secure_storage_adapter.dart';

typedef AuthDevicePlatformResolver = String Function();
typedef AuthDeviceNameResolver = String? Function();
typedef AuthDeviceIdFactory = String Function();

final class SecureAuthDeviceContextProvider
    implements AuthDeviceContextProvider {
  SecureAuthDeviceContextProvider(
    this._storage, {
    AuthDevicePlatformResolver? platformResolver,
    AuthDeviceNameResolver? deviceNameResolver,
    AuthDeviceIdFactory? deviceIdFactory,
  })  : _platformResolver = platformResolver ?? _currentPlatform,
        _deviceNameResolver = deviceNameResolver ?? _noDeviceName,
        _deviceIdFactory = deviceIdFactory ?? _generateDeviceId;

  static const String storageKey = 'afwal.auth.device.id.v1';

  final SecureStorageAdapter _storage;
  final AuthDevicePlatformResolver _platformResolver;
  final AuthDeviceNameResolver _deviceNameResolver;
  final AuthDeviceIdFactory _deviceIdFactory;

  @override
  Future<AuthDeviceContext> load() async {
    var deviceId = (await _storage.read(key: storageKey))?.trim();

    if (deviceId == null || deviceId.isEmpty) {
      deviceId = _deviceIdFactory();
      await _storage.write(key: storageKey, value: deviceId);
    }

    return AuthDeviceContext(
      deviceId: deviceId,
      platform: _platformResolver(),
      deviceName: _deviceNameResolver(),
    );
  }

  static String _currentPlatform() {
    if (Platform.isAndroid) {
      return 'android';
    }
    if (Platform.isIOS) {
      return 'ios';
    }
    if (Platform.isMacOS) {
      return 'macos';
    }
    if (Platform.isWindows) {
      return 'windows';
    }
    if (Platform.isLinux) {
      return 'linux';
    }
    return 'unknown';
  }

  static String? _noDeviceName() => null;

  static String _generateDeviceId() {
    final random = Random.secure();
    final bytes = List<int>.generate(16, (_) => random.nextInt(256));
    return bytes.map((byte) => byte.toRadixString(16).padLeft(2, '0')).join();
  }
}
