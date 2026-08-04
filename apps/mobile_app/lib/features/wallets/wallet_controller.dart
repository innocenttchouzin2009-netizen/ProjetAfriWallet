import 'package:flutter/foundation.dart';
import 'wallet_models.dart';
import 'wallet_service.dart';

class WalletController extends ChangeNotifier {
  WalletController({WalletApiClient? apiClient}) : _apiClient = apiClient ?? WalletApiClient();

  final WalletApiClient _apiClient;
  bool isLoading = false;
  bool privacyMode = false;
  String? error;
  List<WalletSummary> wallets = const [];
  WalletDetail? selectedWallet;
  WalletPortfolioSummary? portfolio;

  Future<void> loadWallets(String awid) async {
    isLoading = true;
    error = null;
    notifyListeners();

    try {
      wallets = await _apiClient.listWallets(awid);
      portfolio = await _apiClient.getPortfolioSummary(awid);
    } catch (e) {
      error = e.toString();
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  Future<void> selectWallet(String walletId) async {
    isLoading = true;
    error = null;
    notifyListeners();

    try {
      selectedWallet = await _apiClient.getWalletDetail(walletId);
    } catch (e) {
      error = e.toString();
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  void togglePrivacyMode() {
    privacyMode = !privacyMode;
    notifyListeners();
  }

  Future<bool> createWallet({
    required String awid,
    required String walletType,
    required String currency,
    String? name,
  }) async {
    isLoading = true;
    error = null;
    notifyListeners();

    try {
      final created = await _apiClient.createWallet(
        awid: awid,
        walletType: walletType,
        currency: currency,
        name: name,
      );
      wallets = [created, ...wallets];
      return true;
    } catch (e) {
      error = e.toString();
      return false;
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }
}
