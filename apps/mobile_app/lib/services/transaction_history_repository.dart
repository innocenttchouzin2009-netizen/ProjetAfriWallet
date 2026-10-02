import '../data/remote/transaction_history_contracts.dart';
import '../data/remote/transaction_history_remote_data_source.dart';
import '../models/transaction_history.dart';
import 'secure_session_store.dart';

class TransactionHistoryPageResult {
  const TransactionHistoryPageResult({
    required this.items,
    required this.nextCursor,
  });

  final List<TransactionHistoryItem> items;
  final String? nextCursor;
}

abstract class TransactionHistoryRepository {
  Future<List<TransactionHistoryItem>> listTransactions();
}

abstract class PaginatedTransactionHistoryRepository
    implements TransactionHistoryRepository {
  Future<TransactionHistoryPageResult> listTransactionPage({
    int? limit,
    String? cursor,
  });
}

class TransactionHistoryUnavailableException implements Exception {
  const TransactionHistoryUnavailableException(this.message);
  final String message;

  @override
  String toString() => message;
}

class UnavailableTransactionHistoryRepository
    implements PaginatedTransactionHistoryRepository {
  const UnavailableTransactionHistoryRepository();

  @override
  Future<List<TransactionHistoryItem>> listTransactions() {
    return Future<List<TransactionHistoryItem>>.error(
      const TransactionHistoryUnavailableException(
        'Transaction history is unavailable. No transaction data is simulated.',
      ),
    );
  }

  @override
  Future<TransactionHistoryPageResult> listTransactionPage({
    int? limit,
    String? cursor,
  }) {
    return Future<TransactionHistoryPageResult>.error(
      const TransactionHistoryUnavailableException(
        'Transaction history is unavailable. No transaction data is simulated.',
      ),
    );
  }
}

class AuthenticatedTransactionHistoryRepository
    implements PaginatedTransactionHistoryRepository {
  const AuthenticatedTransactionHistoryRepository(
    this._remoteDataSource,
    this._sessionStore,
  );

  final TransactionHistoryRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;

  @override
  Future<List<TransactionHistoryItem>> listTransactions() async {
    final page = await listTransactionPage();
    return page.items;
  }

  @override
  Future<TransactionHistoryPageResult> listTransactionPage({
    int? limit,
    String? cursor,
  }) async {
    final session = await _sessionStore.read();
    if (session == null) {
      throw const TransactionHistoryUnavailableException(
        'An authenticated session is required to load transaction history.',
      );
    }

    final page = await _remoteDataSource.listTransactions(
      session.accessToken,
      limit: limit,
      cursor: cursor,
    );
    return TransactionHistoryPageResult(
      items: page.items.map(_mapItem).toList(growable: false),
      nextCursor: page.nextCursor,
    );
  }

  TransactionHistoryItem _mapItem(TransactionHistoryRemoteItem item) {
    return TransactionHistoryItem(
      transactionId: item.transactionId,
      amountMinor: item.amountMinor,
      currencyCode: item.currencyCode,
      direction: switch (item.direction) {
        TransactionHistoryRemoteDirection.incoming =>
          TransactionDirection.incoming,
        TransactionHistoryRemoteDirection.outgoing =>
          TransactionDirection.outgoing,
      },
      status: switch (item.status) {
        TransactionHistoryRemoteStatus.pending =>
          TransactionHistoryStatus.pending,
        TransactionHistoryRemoteStatus.completed =>
          TransactionHistoryStatus.completed,
        TransactionHistoryRemoteStatus.failed => TransactionHistoryStatus.failed,
        TransactionHistoryRemoteStatus.cancelled =>
          TransactionHistoryStatus.cancelled,
        TransactionHistoryRemoteStatus.reversed =>
          TransactionHistoryStatus.reversed,
      },
      occurredAt: item.occurredAtUtc,
      reference: item.reference,
      counterpartyLabel: item.counterpartyLabel,
    );
  }
}
