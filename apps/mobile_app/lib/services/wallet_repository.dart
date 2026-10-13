import '../data/remote/wallet_remote_data_source.dart';
import '../models/wallet_balance.dart';
import '../network/api_exception.dart';
import 'authenticated_request_executor.dart';

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
    this._requestExecutor,
  );

  final WalletRemoteDataSource _remoteDataSource;
  final AuthenticatedRequestExecutor _requestExecutor;

  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    try {
      return await _requestExecutor.execute(
        _remoteDataSource.loadWalletBalances,
      );
    } on ApiUnauthorizedException {
      throw const WalletUnavailableException();
    }
  }
}
