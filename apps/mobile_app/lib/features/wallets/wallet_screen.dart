import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../subscriptions/presentation/screens/subscriptions_screen.dart';
import 'wallet_controller.dart';
import 'wallet_create_screen.dart';

class WalletScreen extends StatelessWidget {
  const WalletScreen({super.key, this.controller});

  final WalletController? controller;

  @override
  Widget build(BuildContext context) {
    if (controller != null) {
      return ChangeNotifierProvider.value(
        value: controller!..loadWallets('demo-awid'),
        child: const WalletScreenContent(),
      );
    }

    return ChangeNotifierProvider(
      create: (_) => WalletController()..loadWallets('demo-awid'),
      child: const WalletScreenContent(),
    );
  }
}

class WalletScreenContent extends StatelessWidget {
  const WalletScreenContent({super.key});

  String _displayValue(WalletController controller, double value) => controller.privacyMode ? '••••' : value.toStringAsFixed(2);

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<WalletController>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Wallets'),
        actions: [
          IconButton(
            icon: Icon(controller.privacyMode ? Icons.visibility_off : Icons.visibility),
            onPressed: controller.togglePrivacyMode,
          ),
          IconButton(
            icon: const Icon(Icons.add_circle_outline),
            onPressed: () async {
              final created = await Navigator.of(context).push<bool>(
                MaterialPageRoute(
                  builder: (_) => WalletCreateScreen(controller: context.read<WalletController>()),
                ),
              );
              if (created == true) {
                await context.read<WalletController>().loadWallets('demo-awid');
              }
            },
          ),
        ],
      ),
      body: controller.isLoading
          ? const Center(child: CircularProgressIndicator())
          : controller.error != null
              ? Center(child: Text(controller.error!))
              : RefreshIndicator(
                  onRefresh: () => controller.loadWallets('demo-awid'),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      if (controller.portfolio != null)
                        Card(
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text('Portfolio', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                                const SizedBox(height: 8),
                                Text('Wallets: ${controller.portfolio!.walletCount}'),
                                Text('Currencies: ${controller.portfolio!.currencyCount}'),
                                Text('Available: ${_displayValue(controller, controller.portfolio!.totalAvailable)}'),
                              ],
                            ),
                          ),
                        ),
                      const SizedBox(height: 16),
                      FilledButton.tonalIcon(
                        onPressed: () {
                          Navigator.of(context).push(
                            MaterialPageRoute<void>(
                              builder: (_) => const SubscriptionsScreen(),
                            ),
                          );
                        },
                        icon: const Icon(Icons.subscriptions_outlined),
                        label: const Text('Abonnements'),
                      ),
                      const SizedBox(height: 16),
                      ...controller.wallets.map((wallet) => Card(
                            margin: const EdgeInsets.only(bottom: 12),
                            child: ListTile(
                              title: Text(wallet.walletNumber),
                              subtitle: Text('${wallet.currency} • ${wallet.walletType}'),
                              trailing: Text('${_displayValue(controller, wallet.availableBalance)} ${wallet.currency}'),
                              onTap: () => Navigator.of(context).pushNamed('/wallets/${wallet.id}'),
                            ),
                          )),
                      if (controller.selectedWallet != null) ...[
                        const SizedBox(height: 16),
                        Card(
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(controller.selectedWallet!.walletNumber, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                                const SizedBox(height: 8),
                                Text('Available: ${_displayValue(controller, controller.selectedWallet!.availableBalance)}'),
                                Text('Ledger: ${_displayValue(controller, controller.selectedWallet!.ledgerBalance)}'),
                                const SizedBox(height: 8),
                                Text('Timeline items: ${controller.selectedWallet!.timeline.length}'),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
    );
  }
}
