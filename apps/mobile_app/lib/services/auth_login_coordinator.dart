import '../models/auth_requests.dart';
import '../models/auth_session.dart';
import 'auth_device_identity.dart';
import 'auth_repository.dart';

class AuthLoginCoordinator {
  const AuthLoginCoordinator(
    this._repository,
    this._deviceIdentity,
  );

  final AuthRepository _repository;
  final AuthDeviceIdentityProvider _deviceIdentity;

  Future<StoredAuthSession> login({
    required String identifier,
    required String password,
    required String platform,
    String? deviceName,
  }) async {
    final deviceId = await _deviceIdentity.getOrCreate();

    return _repository.login(
      AuthLoginRequest(
        identifier: identifier,
        password: password,
        deviceId: deviceId,
        platform: platform,
        deviceName: deviceName,
      ),
    );
  }
}
