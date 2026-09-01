import 'package:flutter/material.dart';

import '../models/wallet_balance.dart';
import '../services/wallet_repository.dart';

class WalletHomePage extends StatefulWidget {
  const WalletHomePage({
    super.key,
    required this.repository,
    this.onContinue,
  });

  final WalletRepository repository;
  final VoidCallback? onContinue;

  @override
  State<WalletHomePage> createState() => _WalletHomePageState();
}

class _WalletHomePageState extends State<WalletHomePage> {
  late Future<List<WalletBalance>> _balances;

  @override
  void initState() {
    super.initState();
    _balances = widget.repository.loadWalletBalances();
  }

  void _retry() {
    setState(() => _balances = widget.repository.loadWalletBalances());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Mon portefeuille')),
      body: SafeArea(
        child: FutureBuilder<List<WalletBalance>>(
          future: _balances,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError) {
              return _UnavailableState(onRetry: _retry, onContinue: widget.onContinue);
            }
            final wallets = snapshot.data ?? const <WalletBalance>[];
            if (wallets.isEmpty) {
              return _EmptyState(onContinue: widget.onContinue);
            }
            return ListView(
              padding: const EdgeInsets.all(20),
              children: [
                Text('Wallet Home', style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 8),
                const Text('Vos soldes confirmés, par devise.'),
                const SizedBox(height: 20),
                for (final wallet in wallets) ...[
                  _WalletCard(wallet: wallet),
                  const SizedBox(height: 12),
                ],
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: null,
                        icon: const Icon(Icons.north_east),
                        label: const Text('Envoyer bientôt'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: null,
                        icon: const Icon(Icons.south_west),
                        label: const Text('Recevoir bientôt'),
                      ),
                    ),
                  ],
                ),
                if (widget.onContinue != null) ...[
                  const SizedBox(height: 20),
                  FilledButton(onPressed: widget.onContinue, child: const Text('Continuer')),
                ],
              ],
            );
          },
        ),
      ),
    );
  }
}

class _WalletCard extends StatelessWidget {
  const _WalletCard({required this.wallet});
  final WalletBalance wallet;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(wallet.currency, style: Theme.of(context).textTheme.titleLarge),
                Text(wallet.isAvailable ? 'Disponible' : wallet.status),
              ],
            ),
            const SizedBox(height: 12),
            Text(wallet.formattedAmount, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 6),
            Text('Wallet ${wallet.walletId}'),
            if (wallet.countryCode != null) Text('Pays ${wallet.countryCode}'),
          ],
        ),
      ),
    );
  }
}

class _UnavailableState extends StatelessWidget {
  const _UnavailableState({required this.onRetry, this.onContinue});
  final VoidCallback onRetry;
  final VoidCallback? onContinue;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.account_balance_wallet_outlined, size: 52),
            const SizedBox(height: 16),
            const Text('Soldes indisponibles'),
            const SizedBox(height: 8),
            const Text('Aucun solde n’est simulé. AfWal affiche uniquement les données confirmées par les services wallet.'),
            const SizedBox(height: 16),
            FilledButton(onPressed: onRetry, child: const Text('Réessayer')),
            if (onContinue != null) TextButton(onPressed: onContinue, child: const Text('Continuer sans solde')),
          ],
        ),
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({this.onContinue});
  final VoidCallback? onContinue;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text('Aucun wallet disponible'),
            const SizedBox(height: 8),
            const Text('Un wallet apparaîtra ici lorsqu’il sera fourni par le registre wallet.'),
            if (onContinue != null) ...[
              const SizedBox(height: 16),
              FilledButton(onPressed: onContinue, child: const Text('Continuer')),
            ],
          ],
        ),
      ),
    );
  }
}
