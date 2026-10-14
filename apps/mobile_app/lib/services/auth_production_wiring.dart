import '../models/auth_requests.dart';
import '../network/api_client.dart';
import 'api_config.dart';
import 'auth_device_context_provider.dart';
import 'auth_remote_data_source.dart';
import 'auth_repository.dart';
import 'auth_session_coordinator.dart';
import 'auth_state_controller.dart';
import 'authenticated_request_executor.dart';
import 'flutter_secure_storage_adapter.dart';
import 'secure_auth_device_context_provider.dart';
import 'secure_session_store.dart';
import 'secure_storage_adapter.dart';

class AuthProductionWiring {
  AuthProductionWiring({
    ApiClient? apiClient,
    SecureStorageAdapter? secureStorageAdapter,
    AuthDeviceContextProvider? authDeviceContextProvider,
  })  : _apiClient = apiClient ?? ApiClient(baseUrl: ApiConfig.baseUrl),
        _ownsApiClient = apiClient == null {
    final storage =
        secureStorageAdapter ?? const FlutterSecureStorageAdapter();

    sessionStore = SecureSessionStore(storage);
    deviceContextProvider = authDeviceContextProvider ??
        SecureAuthDeviceContextProvider(storage);
    repository = RemoteAuthRepository(
      AuthRemoteDataSource(_apiClient),
      sessionStore,
    );
    sessionLifecycle = AuthSessionCoordinator(repository, sessionStore);
    requestExecutor = AuthenticatedRequestExecutor(sessionLifecycle);
    stateController = AuthStateController(
      sessionLifecycle,
      login: repository.login,
    );
  }

  final ApiClient _apiClient;
  final bool _ownsApiClient;

  late final AuthSessionStore sessionStore;
  late final AuthDeviceContextProvider deviceContextProvider;
  late final AuthRepository repository;
  late final AuthSessionLifecycle sessionLifecycle;
  late final AuthenticatedRequestExecutor requestExecutor;
  late final AuthStateController stateController;

  Future<void> login({
    required String identifier,
    required String password,
  }) async {
    final deviceContext = await deviceContextProvider.load();

    await stateController.login(
      AuthLoginRequest(
        identifier: identifier,
        password: password,
        deviceId: deviceContext.deviceId,
        platform: deviceContext.platform,
        deviceName: deviceContext.deviceName,
      ),
    );
  }

  void dispose() {
    stateController.dispose();
    if (_ownsApiClient) {
      _apiClient.close();
    }
  }
}
