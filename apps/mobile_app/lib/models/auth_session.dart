class AuthSession {
  const AuthSession({
    required this.accessToken,
    required this.refreshToken,
    required this.tokenType,
    required this.expiresIn,
    required this.sessionId,
    required this.userId,
  });

  final String accessToken;
  final String refreshToken;
  final String tokenType;
  final int expiresIn;
  final String sessionId;
  final String userId;

  factory AuthSession.fromJson(Map<String, Object?> json) {
    return AuthSession(
      accessToken: _requireString(json, 'accessToken'),
      refreshToken: _requireString(json, 'refreshToken'),
      tokenType: _requireString(json, 'tokenType'),
      expiresIn: _requireInt(json, 'expiresIn'),
      sessionId: _requireString(json, 'sessionId'),
      userId: _requireString(json, 'userId'),
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
}
