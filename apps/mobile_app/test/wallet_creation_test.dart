import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_controller.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_create_screen.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_models.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_service.dart';

class FakeWalletApiClient extends WalletApiClient {
  @override
  Future<List<WalletSummary>> listWallets(String awid) async => const [];

  @override
  Future<WalletSummary> createWallet({
    required String awid,
    required String walletType,
    required String currency,
    String? name,
  }) async {
    return WalletSummary(
      id: 'wallet-1',
      walletNumber: 'AFW-EUR-1234',
      currency: currency,
      walletType: walletType,
      status: 'Created',
      availableBalance: 0,
      pendingBalance: 0,
      reservedBalance: 0,
      createdAt: DateTime.now(),
      updatedAt: DateTime.now(),
    );
  }
}

void main() {
  testWidgets('shows validation when currency is missing', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    await tester.pumpWidget(
      MaterialApp(
        home: WalletCreateScreen(controller: controller),
      ),
    );

    await tester.tap(find.text('Create wallet'));
    await tester.pump();

    expect(find.text('Currency is required'), findsOneWidget);
  });
}
