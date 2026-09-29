import '../network/api_client.dart';
import 'api_config.dart';
import 'auth_remote_data_source.dart';
import 'auth_repository.dart';
import 'auth_session_coordinator.dart';
import 'auth_state_controller.dart';
import 'flutter_secure_storage_adapter.dart';
import 'secure_session_store.dart';
import 'secure_storage_adapter.dart';

class AuthProductionWiring {
  AuthProductionWiring({
    ApiClient? apiClient,
    SecureStorageAdapter? secureStorageAdapter,
  })  : _apiClient = apiClient ?? ApiClient(baseUrl: ApiConfig.baseUrl),
        _ownsApiClient = apiClient == null {
    sessionStore = SecureSessionStore(
      secureStorageAdapter ?? const FlutterSecureStorageAdapter(),
    );
    repository = RemoteAuthRepository(
      AuthRemoteDataSource(_apiClient),
      sessionStore,
    );
    sessionLifecycle = AuthSessionCoordinator(repository, sessionStore);
    stateController = AuthStateController(sessionLifecycle);
  }

  final ApiClient _apiClient;
  final bool _ownsApiClient;

  late final AuthSessionStore sessionStore;
  late final AuthRepository repository;
  late final AuthSessionLifecycle sessionLifecycle;
  late final AuthStateController stateController;

  void dispose() {
    stateController.dispose();
    if (_ownsApiClient) {
      _apiClient.close();
    }
  }
}
