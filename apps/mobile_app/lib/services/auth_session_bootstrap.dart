import 'auth_state_controller.dart';
import 'flutter_secure_storage_adapter.dart';
import 'secure_session_store.dart';
import 'secure_storage_adapter.dart';

class AuthSessionBootstrap {
  AuthSessionBootstrap._({
    required this.sessionStore,
    required this.controller,
  });

  final AuthSessionStore sessionStore;
  final AuthStateController controller;

  static Future<AuthSessionBootstrap> create({
    SecureStorageAdapter? storage,
  }) async {
    final sessionStore = SecureSessionStore(
      storage ?? FlutterSecureStorageAdapter(),
    );
    final controller = AuthStateController(sessionStore);
    await controller.restore();

    return AuthSessionBootstrap._(
      sessionStore: sessionStore,
      controller: controller,
    );
  }

  void dispose() {
    controller.dispose();
  }
}
