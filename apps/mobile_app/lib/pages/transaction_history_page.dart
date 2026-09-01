import 'package:flutter/material.dart';

import '../models/transaction_history.dart';
import '../services/transaction_history_repository.dart';

class TransactionHistoryPage extends StatefulWidget {
  const TransactionHistoryPage({super.key, required this.repository, required this.onContinue});

  final TransactionHistoryRepository repository;
  final VoidCallback onContinue;

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
    final major = item.amountMinor ~/ 100;
    final minor = (item.amountMinor.abs() % 100).toString().padLeft(2, '0');
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Transactions')),
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
                  const Text('Aucune transaction n’est simulée. Les données doivent provenir du backend AfriWallet.'),
                  const SizedBox(height: 24),
                  FilledButton(onPressed: widget.onContinue, child: const Text('Continuer')),
                ]),
              ),
            );
          }
          final items = snapshot.data ?? const <TransactionHistoryItem>[];
          if (items.isEmpty) {
            return Center(
              child: Column(mainAxisSize: MainAxisSize.min, children: [
                const Text('Aucune transaction'),
                const SizedBox(height: 16),
                FilledButton(onPressed: widget.onContinue, child: const Text('Continuer')),
              ]),
            );
          }
          return Column(children: [
            Expanded(
              child: ListView.separated(
                itemCount: items.length,
                separatorBuilder: (_, __) => const Divider(height: 1),
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
              child: SizedBox(width: double.infinity, child: FilledButton(onPressed: widget.onContinue, child: const Text('Continuer'))),
            ),
          ]);
        },
      ),
    );
  }
}
