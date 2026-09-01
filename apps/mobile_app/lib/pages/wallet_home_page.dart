import 'package:flutter/material.dart';

import '../models/transaction_history.dart';
import '../models/wallet_balance.dart';
import '../services/transaction_history_repository.dart';
import '../services/wallet_repository.dart';
import 'transaction_history_page.dart';

class WalletHomePage extends StatefulWidget {
  const WalletHomePage({
    super.key,
    required this.repository,
    this.transactionHistoryRepository = const UnavailableTransactionHistoryRepository(),
    this.onContinue,
  });

  final WalletRepository repository;
  final TransactionHistoryRepository transactionHistoryRepository;
  final VoidCallback? onContinue;

  @override
  State<WalletHomePage> createState() => _WalletHomePageState();
}

class _WalletHomePageState extends State<WalletHomePage> {
  late Future<List<WalletBalance>> _balances;
  late Future<_TimelineLoadResult> _transactions;

  @override
  void initState() {
    super.initState();
    _balances = widget.repository.loadWalletBalances();
    _transactions = _loadTransactions();
  }

  Future<_TimelineLoadResult> _loadTransactions() async {
    try {
      final items = await widget.transactionHistoryRepository.listTransactions();
      return _TimelineLoadResult(items: items);
    } catch (error) {
      return _TimelineLoadResult(error: error);
    }
  }

  void _retryBalances() {
    setState(() => _balances = widget.repository.loadWalletBalances());
  }

  void _retryTransactions() {
    setState(() => _transactions = _loadTransactions());
  }

  void _openFinancialTimeline() {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (context) => TransactionHistoryPage(
          repository: widget.transactionHistoryRepository,
          onContinue: () => Navigator.of(context).pop(),
        ),
      ),
    );
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
              return _UnavailableState(onRetry: _retryBalances, onContinue: widget.onContinue);
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
                const Text('Vos soldes confirmés et votre activité financière récente.'),
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
                        onPressed: widget.onContinue,
                        icon: const Icon(Icons.north_east),
                        label: const Text('Envoyer'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: widget.onContinue,
                        icon: const Icon(Icons.south_west),
                        label: const Text('Recevoir'),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 28),
                _FinancialTimelineSection(
                  transactions: _transactions,
                  onRetry: _retryTransactions,
                  onOpenAll: _openFinancialTimeline,
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

class _TimelineLoadResult {
  const _TimelineLoadResult({this.items = const <TransactionHistoryItem>[], this.error});

  final List<TransactionHistoryItem> items;
  final Object? error;

  bool get hasError => error != null;
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

class _FinancialTimelineSection extends StatelessWidget {
  const _FinancialTimelineSection({
    required this.transactions,
    required this.onRetry,
    required this.onOpenAll,
  });

  final Future<_TimelineLoadResult> transactions;
  final VoidCallback onRetry;
  final VoidCallback onOpenAll;

  String _amount(TransactionHistoryItem item) {
    final sign = item.direction == TransactionDirection.incoming ? '+' : '-';
    final absoluteMinor = item.amountMinor.abs();
    final major = absoluteMinor ~/ 100;
    final minor = (absoluteMinor % 100).toString().padLeft(2, '0');
    return '$sign$major.$minor ${item.currencyCode}';
  }

  String _status(TransactionHistoryStatus status) => switch (status) {
        TransactionHistoryStatus.pending => 'En attente',
        TransactionHistoryStatus.completed => 'Terminée',
        TransactionHistoryStatus.failed => 'Échouée',
        TransactionHistoryStatus.cancelled => 'Annulée',
        TransactionHistoryStatus.reversed => 'Contre-passée',
      };

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text('Financial Timeline', style: Theme.of(context).textTheme.titleLarge),
            ),
            TextButton(onPressed: onOpenAll, child: const Text('Voir tout')),
          ],
        ),
        const SizedBox(height: 4),
        const Text('Les dernières opérations confirmées de votre portefeuille.'),
        const SizedBox(height: 12),
        FutureBuilder<_TimelineLoadResult>(
          future: transactions,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Padding(
                padding: EdgeInsets.symmetric(vertical: 20),
                child: Center(child: CircularProgressIndicator()),
              );
            }

            final result = snapshot.data ?? const _TimelineLoadResult();
            if (result.hasError) {
              return Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Timeline indisponible'),
                      const SizedBox(height: 6),
                      const Text('Aucune transaction n’est simulée lorsque le service est indisponible.'),
                      const SizedBox(height: 8),
                      TextButton(onPressed: onRetry, child: const Text('Réessayer')),
                    ],
                  ),
                ),
              );
            }

            final items = [...result.items]
              ..sort((a, b) => b.occurredAt.compareTo(a.occurredAt));
            if (items.isEmpty) {
              return const Card(
                child: Padding(
                  padding: EdgeInsets.all(16),
                  child: Text('Aucune activité financière pour le moment.'),
                ),
              );
            }

            final recent = items.take(3).toList(growable: false);
            return Card(
              child: Column(
                children: [
                  for (var index = 0; index < recent.length; index++) ...[
                    _TimelineTile(item: recent[index], amount: _amount(recent[index]), status: _status(recent[index].status)),
                    if (index != recent.length - 1) const Divider(height: 1),
                  ],
                ],
              ),
            );
          },
        ),
      ],
    );
  }
}

class _TimelineTile extends StatelessWidget {
  const _TimelineTile({required this.item, required this.amount, required this.status});

  final TransactionHistoryItem item;
  final String amount;
  final String status;

  @override
  Widget build(BuildContext context) {
    final incoming = item.direction == TransactionDirection.incoming;
    return ListTile(
      leading: CircleAvatar(
        child: Icon(incoming ? Icons.south_west : Icons.north_east),
      ),
      title: Text(item.counterpartyLabel ?? item.reference),
      subtitle: Text(status),
      trailing: Text(amount),
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
