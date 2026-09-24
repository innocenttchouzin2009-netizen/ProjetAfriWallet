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
}
