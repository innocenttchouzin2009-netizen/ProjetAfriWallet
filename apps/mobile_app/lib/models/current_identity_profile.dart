class CurrentIdentityProfile {
  const CurrentIdentityProfile({
    required this.userId,
    required this.identifier,
    required this.createdAtUtc,
  });

  final String userId;
  final String identifier;
  final DateTime createdAtUtc;

  factory CurrentIdentityProfile.fromJson(Map<String, Object?> json) {
    return CurrentIdentityProfile(
      userId: _requireNonEmptyString(json, 'userId'),
      identifier: _requireNonEmptyString(json, 'identifier'),
      createdAtUtc: _requireUtcDateTime(json, 'createdAtUtc'),
    );
  }
}

String _requireNonEmptyString(Map<String, Object?> json, String key) {
  final value = json[key];
  if (value is String && value.trim().isNotEmpty) {
    return value;
  }
  throw FormatException('Missing or invalid $key.');
}

DateTime _requireUtcDateTime(Map<String, Object?> json, String key) {
  final value = json[key];
  if (value is! String || value.isEmpty) {
    throw FormatException('Missing or invalid $key.');
  }

  final parsed = DateTime.tryParse(value);
  if (parsed == null) {
    throw FormatException('Missing or invalid $key.');
  }

  return parsed.toUtc();
}
