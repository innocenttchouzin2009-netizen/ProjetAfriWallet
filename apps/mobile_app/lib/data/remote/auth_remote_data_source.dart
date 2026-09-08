import '../../models/auth_credentials.dart';
import '../../models/auth_error.dart';
import '../../models/auth_session.dart';
import '../../network/api_client.dart';
import '../../network/api_exception.dart';
import '../../network/auth_error_parser.dart';

abstract interface class AuthRemoteDataSource {
  Future<AuthSession> login(AuthCredentials credentials);

  Future<AuthSession> refresh(String refreshToken);

  Future<void> logout(String accessToken);
}

class AuthRemoteDataSourceException implements Exception {
  const AuthRemoteDataSourceException(this.error);

  final AuthError error;

  @override
  String toString() => error.message;
}

class ApiAuthRemoteDataSource implements AuthRemoteDataSource {
  const ApiAuthRemoteDataSource({
    required this._apiClient,
    this._errorParser = const AuthErrorParser(),
  });

  static const String loginPath = '/api/v1/auth/login';
  static const String refreshPath = '/api/v1/auth/refresh';
  static const String logoutPath = '/api/v1/auth/logout';

  final ApiClient _apiClient;
  final AuthErrorParser _errorParser;

  @override
  Future<AuthSession> login(AuthCredentials credentials) async {
    try {
      final response = await _apiClient.postJson(
        loginPath,
        body: credentials.toJson(),
      );
      return _parseSession(response);
    } on ApiException catch (error) {
      throw AuthRemoteDataSourceException(_errorParser.parse(error));
    }
  }

  @override
  Future<AuthSession> refresh(String refreshToken) async {
    try {
      final response = await _apiClient.postJson(
        refreshPath,
        body: <String, Object?>{'refreshToken': refreshToken},
      );
      return _parseSession(response);
    } on ApiException catch (error) {
      throw AuthRemoteDataSourceException(_errorParser.parse(error));
    }
  }

  @override
  Future<void> logout(String accessToken) async {
    try {
      await _apiClient.postJson(
        logoutPath,
        headers: <String, String>{'Authorization': 'Bearer $accessToken'},
      );
    } on ApiException catch (error) {
      throw AuthRemoteDataSourceException(_errorParser.parse(error));
    }
  }

  AuthSession _parseSession(Object? response) {
    if (response is! Map<String, dynamic>) {
      throw const ApiMalformedResponseException(
        'The auth session response must be a JSON object.',
      );
    }

    try {
      return AuthSession.fromJson(Map<String, Object?>.from(response));
    } on FormatException catch (error) {
      throw ApiMalformedResponseException(error.message);
    }
  }
}
