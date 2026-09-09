class CurrentAuthSession {
  const CurrentAuthSession({
    required this.userId,
    required this.sessionId,
    required this.deviceId,
    required this.createdAtUtc,
    required this.expiresAtUtc,
    required this.tokenVersion,
  });

  final String userId;
  final String sessionId;
  final String deviceId;
  final DateTime createdAtUtc;
  final DateTime expiresAtUtc;
  final int tokenVersion;

  factory CurrentAuthSession.fromJson(Map<String, Object?> json) {
    return CurrentAuthSession(
      userId: _requireString(json, 'userId'),
      sessionId: _requireString(json, 'sessionId'),
      deviceId: _requireString(json, 'deviceId'),
      createdAtUtc: _requireUtcDateTime(json, 'createdAtUtc'),
      expiresAtUtc: _requireUtcDateTime(json, 'expiresAtUtc'),
      tokenVersion: _requireInt(json, 'tokenVersion'),
    );
  }

  static String _requireString(Map<String, Object?> json, String key) {
    final value = json[key];
    if (value is String && value.isNotEmpty) {
      return value;
    }
    throw FormatException('Missing or invalid $key.');
  }

  static int _requireInt(Map<String, Object?> json, String key) {
    final value = json[key];
    if (value is int) {
      return value;
    }
    throw FormatException('Missing or invalid $key.');
  }

  static DateTime _requireUtcDateTime(
    Map<String, Object?> json,
    String key,
  ) {
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
}
