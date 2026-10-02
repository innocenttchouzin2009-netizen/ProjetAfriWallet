import '../data/remote/transaction_history_contracts.dart';
import '../data/remote/transaction_history_remote_data_source.dart';
import '../models/transaction_history.dart';
import 'secure_session_store.dart';

abstract class TransactionHistoryRepository {
  Future<List<TransactionHistoryItem>> listTransactions();
}

class TransactionHistoryUnavailableException implements Exception {
  const TransactionHistoryUnavailableException(this.message);
  final String message;

  @override
  String toString() => message;
}

class UnavailableTransactionHistoryRepository implements TransactionHistoryRepository {
  const UnavailableTransactionHistoryRepository();

  @override
  Future<List<TransactionHistoryItem>> listTransactions() {
    return Future<List<TransactionHistoryItem>>.error(
      const TransactionHistoryUnavailableException(
        'Transaction history is unavailable. No transaction data is simulated.',
      ),
    );
  }
}

class AuthenticatedTransactionHistoryRepository
    implements TransactionHistoryRepository {
  const AuthenticatedTransactionHistoryRepository(
    this._remoteDataSource,
    this._sessionStore,
  );

  final TransactionHistoryRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;

  @override
  Future<List<TransactionHistoryItem>> listTransactions() async {
    final session = await _sessionStore.read();
    if (session == null) {
      throw const TransactionHistoryUnavailableException(
        'An authenticated session is required to load transaction history.',
      );
    }

    final page = await _remoteDataSource.listTransactions(session.accessToken);
    return page.items.map(_mapItem).toList(growable: false);
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
