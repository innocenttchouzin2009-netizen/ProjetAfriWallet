import '../data/remote/wallet_remote_data_source.dart';
import '../network/api_client.dart';
import 'api_config.dart';
import 'secure_session_store.dart';
import 'wallet_repository.dart';

class WalletProductionWiring {
  WalletProductionWiring({
    required AuthSessionStore sessionStore,
    ApiClient? apiClient,
  }) : _apiClient = apiClient ?? ApiClient(baseUrl: ApiConfig.baseUrl),
       _ownsApiClient = apiClient == null {
    repository = AuthenticatedWalletRepository(
      WalletRemoteDataSource(_apiClient),
      sessionStore,
    );
  }

  final ApiClient _apiClient;
  final bool _ownsApiClient;
  late final WalletRepository repository;

  void dispose() {
    if (_ownsApiClient) {
      _apiClient.close();
    }
  }
}
