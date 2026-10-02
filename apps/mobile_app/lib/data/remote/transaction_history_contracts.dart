enum TransactionHistoryRemoteDirection {
  incoming,
  outgoing;

  static TransactionHistoryRemoteDirection fromWireValue(String value) {
    return switch (value) {
      'Incoming' => TransactionHistoryRemoteDirection.incoming,
      'Outgoing' => TransactionHistoryRemoteDirection.outgoing,
      _ => throw const FormatException('Invalid transaction history direction.'),
    };
  }
}

enum TransactionHistoryRemoteStatus {
  pending,
  completed,
  failed,
  cancelled,
  reversed;

  static TransactionHistoryRemoteStatus fromWireValue(String value) {
    return switch (value) {
      'Pending' => TransactionHistoryRemoteStatus.pending,
      'Completed' => TransactionHistoryRemoteStatus.completed,
      'Failed' => TransactionHistoryRemoteStatus.failed,
      'Cancelled' => TransactionHistoryRemoteStatus.cancelled,
      'Reversed' => TransactionHistoryRemoteStatus.reversed,
      _ => throw const FormatException('Invalid transaction history status.'),
    };
  }
}

class TransactionHistoryRemoteItem {
  const TransactionHistoryRemoteItem({
    required this.transactionId,
    required this.walletId,
    required this.amountMinor,
    required this.currencyCode,
    required this.direction,
    required this.status,
    required this.occurredAtUtc,
    required this.reference,
    required this.counterpartyLabel,
  });

  final String transactionId;
  final String walletId;
  final int amountMinor;
  final String currencyCode;
  final TransactionHistoryRemoteDirection direction;
  final TransactionHistoryRemoteStatus status;
  final DateTime occurredAtUtc;
  final String reference;
  final String? counterpartyLabel;
}

class TransactionHistoryRemotePage {
  const TransactionHistoryRemotePage({
    required this.items,
    required this.nextCursor,
  });

  final List<TransactionHistoryRemoteItem> items;
  final String? nextCursor;
}
