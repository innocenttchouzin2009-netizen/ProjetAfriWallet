enum AuthErrorCode {
  invalidCredentials('AUTH_INVALID_CREDENTIALS'),
  identifierAlreadyExists('AUTH_IDENTIFIER_ALREADY_EXISTS'),
  sessionExpired('AUTH_SESSION_EXPIRED'),
  sessionRevoked('AUTH_SESSION_REVOKED'),
  refreshInvalid('AUTH_REFRESH_INVALID'),
  refreshExpired('AUTH_REFRESH_EXPIRED'),
  refreshReused('AUTH_REFRESH_REUSED'),
  tokenInvalid('AUTH_TOKEN_INVALID'),
  tokenExpired('AUTH_TOKEN_EXPIRED'),
  userDisabled('AUTH_USER_DISABLED'),
  validationError('AUTH_VALIDATION_ERROR'),
  unknown('AUTH_UNKNOWN');

  const AuthErrorCode(this.wireValue);

  final String wireValue;

  static AuthErrorCode fromWireValue(String value) {
    for (final code in values) {
      if (code.wireValue == value) {
        return code;
      }
    }
    return unknown;
  }
}

class AuthError {
  const AuthError({
    required this.code,
    required this.message,
    this.traceId,
  });

  final AuthErrorCode code;
  final String message;
  final String? traceId;

  factory AuthError.fromJson(Map<String, Object?> json) {
    final code = json['code'];
    final message = json['message'];
    final traceId = json['traceId'];

    if (code is! String || code.isEmpty || message is! String || message.isEmpty) {
      throw const FormatException('Invalid auth error payload.');
    }
    if (traceId != null && traceId is! String) {
      throw const FormatException('Invalid auth error traceId.');
    }

    return AuthError(
      code: AuthErrorCode.fromWireValue(code),
      message: message,
      traceId: traceId as String?,
    );
  }
}
