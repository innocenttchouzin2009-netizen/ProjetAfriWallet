import '../models/transaction_history.dart';

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
