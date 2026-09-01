import '../models/wallet_balance.dart';

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
