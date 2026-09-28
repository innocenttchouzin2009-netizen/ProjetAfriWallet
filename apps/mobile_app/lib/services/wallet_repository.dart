import '../data/remote/wallet_remote_data_source.dart';
import '../models/auth_session.dart';
import '../models/wallet_balance.dart';
import 'auth_session_coordinator.dart';
import 'secure_session_store.dart';

abstract interface class WalletRepository {
  Future<List<WalletBalance>> loadWalletBalances();
}

class WalletUnavailableException implements Exception {
  const WalletUnavailableException();
}

class UnavailableWalletRepository implements WalletRepository {
  const UnavailableWalletRepository();

  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    throw const WalletUnavailableException();
  }
}

class AuthenticatedWalletRepository implements WalletRepository {
  const AuthenticatedWalletRepository(
    this._remoteDataSource,
    this._sessionStore,
  ) : _sessionLifecycle = null;

  const AuthenticatedWalletRepository.withSessionLifecycle(
    this._remoteDataSource,
    this._sessionLifecycle,
  ) : _sessionStore = null;

  final WalletRemoteDataSource _remoteDataSource;
  final AuthSessionStore? _sessionStore;
  final AuthSessionLifecycle? _sessionLifecycle;

  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    final session = await _restoreValidSession();
    if (session == null) {
      throw const WalletUnavailableException();
    }

    return _remoteDataSource.loadWalletBalances(session.accessToken);
  }

  Future<StoredAuthSession?> _restoreValidSession() async {
    final lifecycle = _sessionLifecycle;
    if (lifecycle != null) {
      return lifecycle.restoreValidSession();
    }

    final session = await _sessionStore!.read();
    if (session == null ||
        session.isAccessTokenExpired(DateTime.now().toUtc())) {
      return null;
    }

    return session;
  }
}
