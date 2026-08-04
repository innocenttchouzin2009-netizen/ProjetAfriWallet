import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'wallet_controller.dart';

class WalletDetailScreen extends StatefulWidget {
  const WalletDetailScreen({super.key, required this.walletId, this.controller});

  final String walletId;
  final WalletController? controller;

  @override
  State<WalletDetailScreen> createState() => _WalletDetailScreenState();
}

class _WalletDetailScreenState extends State<WalletDetailScreen> {
  int _visibleCount = 1;

  @override
  void initState() {
    super.initState();
    if (widget.controller != null) {
      Future.microtask(() => widget.controller!.selectWallet(widget.walletId));
    } else {
      Future.microtask(() => context.read<WalletController>().selectWallet(widget.walletId));
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller ?? context.watch<WalletController>();
    final wallet = controller.selectedWallet;

    return Scaffold(
      appBar: AppBar(title: const Text('Wallet detail')),
      body: controller.isLoading
          ? const Center(child: CircularProgressIndicator())
          : wallet == null
              ? const Center(child: Text('No wallet selected'))
              : RefreshIndicator(
                  onRefresh: () async => controller.selectWallet(widget.walletId),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      Text(wallet.walletNumber, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      Text('${wallet.currency} • ${wallet.walletType} • ${wallet.status}'),
                      const SizedBox(height: 16),
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Balance decomposition', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                              const SizedBox(height: 8),
                              Text('Available: ${controller.privacyMode ? '••••' : wallet.availableBalance.toStringAsFixed(2)}'),
                              Text('Pending: ${controller.privacyMode ? '••••' : wallet.pendingBalance.toStringAsFixed(2)}'),
                              Text('Reserved: ${controller.privacyMode ? '••••' : wallet.reservedBalance.toStringAsFixed(2)}'),
                              Text('Ledger: ${controller.privacyMode ? '••••' : wallet.ledgerBalance.toStringAsFixed(2)}'),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Timeline', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                              const SizedBox(height: 8),
                              if (wallet.timeline.isEmpty)
                                const Text('No activity yet.')
                              else ...[
                                ...wallet.timeline.take(_visibleCount).map((item) => ListTile(
                                      leading: Icon(item.direction == '+' ? Icons.arrow_downward : Icons.arrow_upward),
                                      title: Text(item.reference.isEmpty ? item.description : item.reference),
                                      subtitle: Text(item.description),
                                      trailing: Text('${item.direction}${item.amount.toStringAsFixed(2)}'),
                                    )),
                                if (wallet.timeline.length > _visibleCount)
                                  TextButton(
                                    onPressed: () => setState(() => _visibleCount += 1),
                                    child: const Text('Load more'),
                                  ),
                              ],
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }
}
