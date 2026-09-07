import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/network/auth_error_parser.dart';

void main() {
  const parser = AuthErrorParser();

  test('parses stable backend auth error contract', () {
    const exception = ApiUnauthorizedException(
      responseBody:
          '{"code":"AUTH_INVALID_CREDENTIALS","message":"Invalid credentials.","traceId":"trace-123"}',
    );

    final error = parser.parse(exception);

    expect(error.code, AuthErrorCode.invalidCredentials);
    expect(error.message, 'Invalid credentials.');
    expect(error.traceId, 'trace-123');
  });

  test('preserves unknown backend auth error as unknown', () {
    const exception = ApiForbiddenException(
      responseBody: '{"code":"AUTH_FUTURE_CODE","message":"Future error."}',
    );

    final error = parser.parse(exception);

    expect(error.code, AuthErrorCode.unknown);
    expect(error.message, 'Future error.');
  });

  test('falls back to api exception when response body is malformed', () {
    const exception = ApiUnauthorizedException(responseBody: 'not-json');

    final error = parser.parse(exception);

    expect(error.code, AuthErrorCode.unknown);
    expect(error.message, 'Authentication is required.');
    expect(error.traceId, isNull);
  });
}
