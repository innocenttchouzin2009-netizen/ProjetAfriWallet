class AuthSessionResponse {
  const AuthSessionResponse({
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

  factory AuthSessionResponse.fromJson(Map<String, Object?> json) {
    return AuthSessionResponse(
      accessToken: _requireString(json, 'accessToken'),
      refreshToken: _requireString(json, 'refreshToken'),
      tokenType: _requireString(json, 'tokenType'),
      expiresIn: _requirePositiveInt(json, 'expiresIn'),
      sessionId: _requireString(json, 'sessionId'),
      userId: _requireString(json, 'userId'),
    );
  }

  StoredAuthSession toStored({required DateTime receivedAtUtc}) {
    final normalizedReceivedAtUtc = receivedAtUtc.toUtc();
    return StoredAuthSession(
      accessToken: accessToken,
      refreshToken: refreshToken,
      tokenType: tokenType,
      sessionId: sessionId,
      userId: userId,
      accessTokenExpiresAtUtc:
          normalizedReceivedAtUtc.add(Duration(seconds: expiresIn)),
    );
  }
}

class StoredAuthSession {
  const StoredAuthSession({
    required this.accessToken,
    required this.refreshToken,
    required this.tokenType,
    required this.sessionId,
    required this.userId,
    required this.accessTokenExpiresAtUtc,
  });

  final String accessToken;
  final String refreshToken;
  final String tokenType;
  final String sessionId;
  final String userId;
  final DateTime accessTokenExpiresAtUtc;

  bool isAccessTokenExpired(DateTime nowUtc) =>
      !nowUtc.toUtc().isBefore(accessTokenExpiresAtUtc.toUtc());

  Map<String, Object?> toJson() => <String, Object?>{
        'accessToken': accessToken,
        'refreshToken': refreshToken,
        'tokenType': tokenType,
        'sessionId': sessionId,
        'userId': userId,
        'accessTokenExpiresAtUtc': accessTokenExpiresAtUtc.toUtc().toIso8601String(),
      };

  factory StoredAuthSession.fromJson(Map<String, Object?> json) {
    return StoredAuthSession(
      accessToken: _requireString(json, 'accessToken'),
      refreshToken: _requireString(json, 'refreshToken'),
      tokenType: _requireString(json, 'tokenType'),
      sessionId: _requireString(json, 'sessionId'),
      userId: _requireString(json, 'userId'),
      accessTokenExpiresAtUtc:
          _requireUtcDateTime(json, 'accessTokenExpiresAtUtc'),
    );
  }
}

String _requireString(Map<String, Object?> json, String key) {
  final value = json[key];
  if (value is String && value.isNotEmpty) {
    return value;
  }
  throw FormatException('Missing or invalid $key.');
}

int _requirePositiveInt(Map<String, Object?> json, String key) {
  final value = json[key];
  if (value is int && value > 0) {
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
