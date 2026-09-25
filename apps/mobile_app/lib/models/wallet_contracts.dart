class WalletResponse {
  const WalletResponse({
    required this.walletId,
    required this.ownerId,
    required this.currencyCode,
    required this.status,
    required this.createdAtUtc,
    required this.updatedAtUtc,
    this.countryCode,
  });

  final String walletId;
  final String ownerId;
  final String currencyCode;
  final String? countryCode;
  final String status;
  final DateTime createdAtUtc;
  final DateTime updatedAtUtc;

  bool get isActive => status.toUpperCase() == 'ACTIVE';

  factory WalletResponse.fromJson(Map<String, Object?> json) {
    final countryCode = json['countryCode'];
    if (countryCode != null && countryCode is! String) {
      throw const FormatException('Invalid wallet countryCode.');
    }

    return WalletResponse(
      walletId: _requireString(json, 'walletId'),
      ownerId: _requireString(json, 'ownerId'),
      currencyCode: _requireString(json, 'currencyCode'),
      countryCode: countryCode as String?,
      status: _requireString(json, 'status'),
      createdAtUtc: _requireUtcDateTime(json, 'createdAtUtc'),
      updatedAtUtc: _requireUtcDateTime(json, 'updatedAtUtc'),
    );
  }
}

String _requireString(Map<String, Object?> json, String key) {
  final value = json[key];
  if (value is String && value.trim().isNotEmpty) {
    return value;
  }
  throw FormatException('Missing or invalid $key.');
}

DateTime _requireUtcDateTime(Map<String, Object?> json, String key) {
  final value = json[key];
  if (value is! String || value.trim().isEmpty) {
    throw FormatException('Missing or invalid $key.');
  }

  final parsed = DateTime.tryParse(value);
  if (parsed == null) {
    throw FormatException('Missing or invalid $key.');
  }
  return parsed.toUtc();
}
