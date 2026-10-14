import '../models/auth_device_context.dart';

abstract interface class AuthDeviceContextProvider {
  Future<AuthDeviceContext> load();
}
