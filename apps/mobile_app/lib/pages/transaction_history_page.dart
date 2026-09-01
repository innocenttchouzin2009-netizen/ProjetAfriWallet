import 'package:flutter/material.dart';

import '../models/transaction_history.dart';
import '../services/transaction_history_repository.dart';

class TransactionHistoryPage extends StatefulWidget {
  const TransactionHistoryPage({
    super.key,
    required this.repository,
    required this.onReturnToWallet,
    this.onContinue,
  });

  final TransactionHistoryRepository repository;
  final VoidCallback onReturnToWallet;
  final VoidCallback? onContinue;

  @override
  State<TransactionHistoryPage> createState() => _TransactionHistoryPageState();
}

class _TransactionHistoryPageState extends State<TransactionHistoryPage> {
  late Future<List<TransactionHistoryItem>> _transactions;

  @override
  void initState() {
    super.initState();
    _transactions = widget.repository.listTransactions();
  }

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
        TransactionHistoryStatus.reversed => 'Annulée / contre-passée',
      };

  void _openDetails(TransactionHistoryItem item) {
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (context) => Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Détail de la transaction', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 16),
            Text(_amount(item)),
            Text('Statut : ${_status(item.status)}'),
            Text('Référence : ${item.reference}'),
            Text('ID : ${item.transactionId}'),
            if (item.counterpartyLabel != null) Text('Contrepartie : ${item.counterpartyLabel}'),
            Text('Date : ${item.occurredAt.toLocal()}'),
          ],
        ),
      ),
    );
  }

  Widget _navigationActions({required String returnKey}) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        SizedBox(
          width: double.infinity,
          child: FilledButton.icon(
            key: Key(returnKey),
            onPressed: widget.onReturnToWallet,
            icon: const Icon(Icons.account_balance_wallet_outlined),
            label: const Text('Retour au portefeuille'),
          ),
        ),
        if (widget.onContinue != null) ...[
          const SizedBox(height: 8),
          TextButton(onPressed: widget.onContinue, child: const Text('Continuer')),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Financial Timeline')),
      body: FutureBuilder<List<TransactionHistoryItem>>(
        future: _transactions,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(mainAxisSize: MainAxisSize.min, children: [
                  const Icon(Icons.receipt_long_outlined, size: 48),
                  const SizedBox(height: 16),
                  const Text('Historique indisponible', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  const Text('Aucune transaction n’est simulée. Les données doivent provenir du backend AfWal.'),
                  const SizedBox(height: 24),
                  _navigationActions(returnKey: 'return-to-wallet-history-error'),
                ]),
              ),
            );
          }
          final items = snapshot.data ?? const <TransactionHistoryItem>[];
          if (items.isEmpty) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(mainAxisSize: MainAxisSize.min, children: [
                  const Text('Aucune transaction'),
                  const SizedBox(height: 16),
                  _navigationActions(returnKey: 'return-to-wallet-history-empty'),
                ]),
              ),
            );
          }
          return Column(children: [
            Expanded(
              child: ListView.separated(
                itemCount: items.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (context, index) {
                  final item = items[index];
                  return ListTile(
                    leading: Icon(item.direction == TransactionDirection.incoming ? Icons.south_west : Icons.north_east),
                    title: Text(item.counterpartyLabel ?? item.reference),
                    subtitle: Text(_status(item.status)),
                    trailing: Text(_amount(item)),
                    onTap: () => _openDetails(item),
                  );
                },
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(16),
              child: _navigationActions(returnKey: 'return-to-wallet-history-list'),
            ),
          ]);
        },
      ),
    );
  }
}
