class WalletSummary {
  const WalletSummary({
    required this.id,
    required this.walletNumber,
    required this.currency,
    required this.walletType,
    required this.status,
    required this.availableBalance,
    required this.pendingBalance,
    required this.reservedBalance,
    required this.createdAt,
    required this.updatedAt,
  });

  final String id;
  final String walletNumber;
  final String currency;
  final String walletType;
  final String status;
  final double availableBalance;
  final double pendingBalance;
  final double reservedBalance;
  final DateTime createdAt;
  final DateTime updatedAt;

  factory WalletSummary.fromJson(Map<String, dynamic> json) {
    return WalletSummary(
      id: json['id'].toString(),
      walletNumber: json['walletNumber'] as String? ?? '',
      currency: json['currency'] as String? ?? 'EUR',
      walletType: json['walletType'] as String? ?? '',
      status: json['status'] as String? ?? '',
      availableBalance: (json['availableBalance'] as num?)?.toDouble() ?? 0,
      pendingBalance: (json['pendingBalance'] as num?)?.toDouble() ?? 0,
      reservedBalance: (json['reservedBalance'] as num?)?.toDouble() ?? 0,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: DateTime.parse(json['updatedAt'] as String),
    );
  }
}

class WalletDetail {
  const WalletDetail({
    required this.id,
    required this.walletNumber,
    required this.currency,
    required this.walletType,
    required this.status,
    required this.availableBalance,
    required this.pendingBalance,
    required this.reservedBalance,
    required this.ledgerBalance,
    required this.updatedAt,
    required this.timeline,
    required this.lastActivityAt,
  });

  final String id;
  final String walletNumber;
  final String currency;
  final String walletType;
  final String status;
  final double availableBalance;
  final double pendingBalance;
  final double reservedBalance;
  final double ledgerBalance;
  final DateTime updatedAt;
  final List<WalletTimelineItem> timeline;
  final DateTime lastActivityAt;

  factory WalletDetail.fromJson(Map<String, dynamic> json) {
    return WalletDetail(
      id: json['id'].toString(),
      walletNumber: json['walletNumber'] as String? ?? '',
      currency: json['currency'] as String? ?? 'EUR',
      walletType: json['walletType'] as String? ?? '',
      status: json['status'] as String? ?? '',
      availableBalance: (json['availableBalance'] as num?)?.toDouble() ?? 0,
      pendingBalance: (json['pendingBalance'] as num?)?.toDouble() ?? 0,
      reservedBalance: (json['reservedBalance'] as num?)?.toDouble() ?? 0,
      ledgerBalance: (json['ledgerBalance'] as num?)?.toDouble() ?? 0,
      updatedAt: DateTime.parse(json['updatedAt'] as String),
      timeline: (json['timeline'] as List<dynamic>? ?? const [])
          .map((item) => WalletTimelineItem.fromJson(item as Map<String, dynamic>))
          .toList(),
      lastActivityAt: DateTime.parse(json['lastActivityAt'] as String),
    );
  }
}

class WalletTimelineItem {
  const WalletTimelineItem({
    required this.transactionId,
    required this.reference,
    required this.description,
    required this.occurredAt,
    required this.direction,
    required this.amount,
    required this.currency,
  });

  final String transactionId;
  final String reference;
  final String description;
  final DateTime occurredAt;
  final String direction;
  final double amount;
  final String currency;

  factory WalletTimelineItem.fromJson(Map<String, dynamic> json) {
    return WalletTimelineItem(
      transactionId: json['transactionId'].toString(),
      reference: json['reference'] as String? ?? '',
      description: json['description'] as String? ?? '',
      occurredAt: DateTime.parse(json['occurredAt'] as String),
      direction: json['direction'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      currency: json['currency'] as String? ?? 'EUR',
    );
  }
}

class WalletPortfolioSummary {
  const WalletPortfolioSummary({
    required this.awid,
    required this.walletCount,
    required this.totalAvailable,
    required this.totalLedgerBalance,
    required this.currencyCount,
    required this.generatedAt,
  });

  final String awid;
  final int walletCount;
  final double totalAvailable;
  final double totalLedgerBalance;
  final int currencyCount;
  final DateTime generatedAt;

  factory WalletPortfolioSummary.fromJson(Map<String, dynamic> json) {
    return WalletPortfolioSummary(
      awid: json['awid'] as String? ?? '',
      walletCount: json['walletCount'] as int? ?? 0,
      totalAvailable: (json['totalAvailable'] as num?)?.toDouble() ?? 0,
      totalLedgerBalance: (json['totalLedgerBalance'] as num?)?.toDouble() ?? 0,
      currencyCount: json['currencyCount'] as int? ?? 0,
      generatedAt: DateTime.parse(json['generatedAt'] as String),
    );
  }
}
