import '../data/remote/identity_remote_data_source.dart';
import '../data/remote/p2p_remote_data_source.dart';
import '../data/remote/qr_payment_remote_data_source.dart';
import '../data/remote/transaction_history_remote_data_source.dart';
import '../network/api_client.dart';
import 'api_config.dart';
import 'auth_production_wiring.dart';
import 'identity_read_repository.dart';
import 'qr_payment_repository.dart';
import 'secure_session_store.dart';
import 'secure_storage_adapter.dart';
import 'transfer_repository.dart';
import 'transaction_history_repository.dart';
import 'wallet_production_wiring.dart';
import 'wallet_repository.dart';

class AuthenticatedProductionWiring {
  AuthenticatedProductionWiring({
    ApiClient? apiClient,
    SecureStorageAdapter? secureStorageAdapter,
  })  : _apiClient = apiClient ?? ApiClient(baseUrl: ApiConfig.baseUrl),
        _ownsApiClient = apiClient == null {
    auth = AuthProductionWiring(
      apiClient: _apiClient,
      secureStorageAdapter: secureStorageAdapter,
    );
    identityReadRepository = RemoteIdentityReadRepository(
      IdentityRemoteDataSource(_apiClient),
      auth.sessionStore,
    );
    wallet = WalletProductionWiring(
      sessionStore: auth.sessionStore,
      apiClient: _apiClient,
    );
    transferRepository = AuthenticatedTransferRepository(
      P2PRemoteDataSource(_apiClient),
      auth.sessionStore,
    );
    transactionHistoryRepository = AuthenticatedTransactionHistoryRepository(
      TransactionHistoryRemoteDataSource(_apiClient),
      auth.sessionStore,
    );
    qrPaymentRepository = AuthenticatedQrPaymentRepository(
      QrPaymentRemoteDataSource(_apiClient),
      auth.sessionStore,
    );
  }

  final ApiClient _apiClient;
  final bool _ownsApiClient;

  late final AuthProductionWiring auth;
  late final IdentityReadRepository identityReadRepository;
  late final WalletProductionWiring wallet;
  late final TransferRepository transferRepository;
  late final TransactionHistoryRepository transactionHistoryRepository;
  late final QrPaymentRepository qrPaymentRepository;

  AuthSessionStore get sessionStore => auth.sessionStore;
  WalletRepository get walletRepository => wallet.repository;

  void dispose() {
    wallet.dispose();
    auth.dispose();
    if (_ownsApiClient) {
      _apiClient.close();
    }
  }
}
