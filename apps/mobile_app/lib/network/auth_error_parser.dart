import 'dart:convert';

import '../models/auth_error.dart';
import 'api_exception.dart';

class AuthErrorParser {
  const AuthErrorParser();

  AuthError parse(ApiException exception) {
    if (exception is ApiHttpException) {
      final parsed = _tryParseBody(exception.responseBody);
      if (parsed != null) {
        return parsed;
      }
    }

    return AuthError(
      code: AuthErrorCode.unknown,
      message: exception.message,
    );
  }

  AuthError? _tryParseBody(String? responseBody) {
    if (responseBody == null || responseBody.trim().isEmpty) {
      return null;
    }

    try {
      final decoded = jsonDecode(responseBody);
      if (decoded is! Map<String, dynamic>) {
        return null;
      }
      return AuthError.fromJson(decoded.cast<String, Object?>());
    } on FormatException {
      return null;
    }
  }
}
