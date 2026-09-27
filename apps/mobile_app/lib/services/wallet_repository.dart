import '../data/remote/wallet_remote_data_source.dart';
import '../models/wallet_balance.dart';
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
  );

  final WalletRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;

  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    final session = await _sessionStore.read();
    if (session == null) {
      throw const WalletUnavailableException();
    }

    return _remoteDataSource.loadWalletBalances(session.accessToken);
  }
}
