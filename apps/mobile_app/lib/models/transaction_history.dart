enum TransactionDirection { incoming, outgoing }

enum TransactionHistoryStatus { pending, completed, failed, cancelled, reversed }

class TransactionHistoryItem {
  const TransactionHistoryItem({
    required this.transactionId,
    required this.amountMinor,
    required this.currencyCode,
    required this.direction,
    required this.status,
    required this.occurredAt,
    required this.reference,
    this.counterpartyLabel,
  });

  final String transactionId;
  final int amountMinor;
  final String currencyCode;
  final TransactionDirection direction;
  final TransactionHistoryStatus status;
  final DateTime occurredAt;
  final String reference;
  final String? counterpartyLabel;
}
