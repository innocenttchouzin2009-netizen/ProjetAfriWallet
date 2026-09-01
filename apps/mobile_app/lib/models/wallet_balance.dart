class WalletBalance {
  const WalletBalance({
    required this.walletId,
    required this.currency,
    required this.availableMinor,
    required this.status,
    this.countryCode,
  });

  final String walletId;
  final String currency;
  final int availableMinor;
  final String status;
  final String? countryCode;

  bool get isAvailable => status.toUpperCase() == 'ACTIVE';

  String get formattedAmount {
    final sign = availableMinor < 0 ? '-' : '';
    final absolute = availableMinor.abs();
    final units = absolute ~/ 100;
    final cents = (absolute % 100).toString().padLeft(2, '0');
    return '$sign$units.$cents $currency';
  }
}
