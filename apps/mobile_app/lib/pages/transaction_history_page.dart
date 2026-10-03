import 'dart:async';

import 'package:flutter/material.dart';

import '../models/transaction_history.dart';
import '../presentation/transaction_history_pagination_controller.dart';
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
  TransactionHistoryPaginationController? _paginationController;
  Future<List<TransactionHistoryItem>>? _legacyTransactions;

  @override
  void initState() {
    super.initState();
    _configureRepository();
  }

  @override
  void didUpdateWidget(covariant TransactionHistoryPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!identical(oldWidget.repository, widget.repository)) {
      _paginationController?.dispose();
      _configureRepository();
    }
  }

  @override
  void dispose() {
    _paginationController?.dispose();
    super.dispose();
  }

  void _configureRepository() {
    final repository = widget.repository;
    if (repository is PaginatedTransactionHistoryRepository) {
      final controller = TransactionHistoryPaginationController(
        repository: repository,
      );
      _paginationController = controller;
      _legacyTransactions = null;
      unawaited(controller.loadInitial());
      return;
    }

    _paginationController = null;
    _legacyTransactions = repository.listTransactions();
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
            Text(
              'Détail de la transaction',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 16),
            Text(_amount(item)),
            Text('Statut : ${_status(item.status)}'),
            Text('Référence : ${item.reference}'),
            Text('ID : ${item.transactionId}'),
            if (item.counterpartyLabel != null)
              Text('Contrepartie : ${item.counterpartyLabel}'),
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
          TextButton(
            onPressed: widget.onContinue,
            child: const Text('Continuer'),
          ),
        ],
      ],
    );
  }

  Widget _initialLoading() {
    return const Center(
      key: Key('transaction-history-initial-loading'),
      child: CircularProgressIndicator(),
    );
  }

  Widget _initialError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.receipt_long_outlined, size: 48),
            const SizedBox(height: 16),
            const Text(
              'Historique indisponible',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Aucune transaction n’est simulée. '
              'Les données doivent provenir du backend AfWal.',
            ),
            const SizedBox(height: 24),
            _navigationActions(returnKey: 'return-to-wallet-history-error'),
          ],
        ),
      ),
    );
  }

  Widget _emptyHistory() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text('Aucune transaction'),
            const SizedBox(height: 16),
            _navigationActions(returnKey: 'return-to-wallet-history-empty'),
          ],
        ),
      ),
    );
  }

  Widget _paginationFooter(TransactionHistoryPaginationController controller) {
    if (controller.isLoadingMore) {
      return const Row(
        key: Key('transaction-history-load-more-loading'),
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          SizedBox(
            width: 18,
            height: 18,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
          SizedBox(width: 12),
          Text('Chargement de la suite…'),
        ],
      );
    }

    if (controller.paginationError != null) {
      return Column(
        key: const Key('transaction-history-pagination-error'),
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text('Impossible de charger plus de transactions.'),
          TextButton(
            key: const Key('transaction-history-pagination-retry'),
            onPressed: () => unawaited(controller.loadMore()),
            child: const Text('Réessayer'),
          ),
        ],
      );
    }

    if (controller.hasMore) {
      return OutlinedButton.icon(
        key: const Key('transaction-history-load-more'),
        onPressed: () => unawaited(controller.loadMore()),
        icon: const Icon(Icons.expand_more),
        label: const Text('Charger plus'),
      );
    }

    return const Text(
      'Fin de l’historique',
      key: Key('transaction-history-end'),
      textAlign: TextAlign.center,
    );
  }

  Widget _historyList(
    List<TransactionHistoryItem> items, {
    Widget? paginationFooter,
  }) {
    return Column(
      children: [
        Expanded(
          child: ListView.separated(
            itemCount: items.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, index) {
              final item = items[index];
              return ListTile(
                leading: Icon(
                  item.direction == TransactionDirection.incoming
                      ? Icons.south_west
                      : Icons.north_east,
                ),
                title: Text(item.counterpartyLabel ?? item.reference),
                subtitle: Text(_status(item.status)),
                trailing: Text(_amount(item)),
                onTap: () => _openDetails(item),
              );
            },
          ),
        ),
        if (paginationFooter != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: paginationFooter,
          ),
        Padding(
          padding: const EdgeInsets.all(16),
          child: _navigationActions(
            returnKey: 'return-to-wallet-history-list',
          ),
        ),
      ],
    );
  }

  Widget _paginatedBody(TransactionHistoryPaginationController controller) {
    if (controller.isInitialLoading && controller.items.isEmpty) {
      return _initialLoading();
    }
    if (controller.initialError != null && controller.items.isEmpty) {
      return _initialError();
    }
    if (controller.items.isEmpty) {
      return _emptyHistory();
    }

    return _historyList(
      controller.items,
      paginationFooter: _paginationFooter(controller),
    );
  }

  Widget _legacyBody() {
    return FutureBuilder<List<TransactionHistoryItem>>(
      future: _legacyTransactions,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return _initialLoading();
        }
        if (snapshot.hasError) {
          return _initialError();
        }

        final items = snapshot.data ?? const <TransactionHistoryItem>[];
        if (items.isEmpty) {
          return _emptyHistory();
        }
        return _historyList(items);
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final controller = _paginationController;
    return Scaffold(
      appBar: AppBar(title: const Text('Financial Timeline')),
      body: controller == null
          ? _legacyBody()
          : ListenableBuilder(
              listenable: controller,
              builder: (context, _) => _paginatedBody(controller),
            ),
    );
  }
}
