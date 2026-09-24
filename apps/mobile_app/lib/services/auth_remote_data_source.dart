import '../models/auth_requests.dart';
import '../models/auth_session.dart';
import '../models/current_auth_session.dart';
import '../network/api_client.dart';

class AuthRemoteDataSource {
  AuthRemoteDataSource(this._apiClient);

  static const String _loginPath = '/api/v1/auth/login';
  static const String _refreshPath = '/api/v1/auth/refresh';
  static const String _logoutPath = '/api/v1/auth/logout';
  static const String _logoutAllPath = '/api/v1/auth/logout-all';
  static const String _sessionPath = '/api/v1/auth/session';

  final ApiClient _apiClient;

  Future<AuthSessionResponse> login(AuthLoginRequest request) async {
    final payload = await _apiClient.postJson(
      _loginPath,
      body: request.toJson(),
    );
    return AuthSessionResponse.fromJson(_requireObject(payload));
  }

  Future<AuthSessionResponse> refresh(AuthRefreshRequest request) async {
    final payload = await _apiClient.postJson(
      _refreshPath,
      body: request.toJson(),
    );
    return AuthSessionResponse.fromJson(_requireObject(payload));
  }

  Future<void> logout(String accessToken) async {
    await _apiClient.postJson(
      _logoutPath,
      headers: _bearerHeaders(accessToken),
    );
  }

  Future<void> logoutAll(String accessToken) async {
    await _apiClient.postJson(
      _logoutAllPath,
      headers: _bearerHeaders(accessToken),
    );
  }

  Future<CurrentAuthSession> loadCurrentSession(String accessToken) async {
    final payload = await _apiClient.getJson(
      _sessionPath,
      headers: _bearerHeaders(accessToken),
    );
    return CurrentAuthSession.fromJson(_requireObject(payload));
  }

  Map<String, String> _bearerHeaders(String accessToken) => <String, String>{
        'Authorization': 'Bearer $accessToken',
      };

  Map<String, Object?> _requireObject(Object? payload) {
    if (payload is Map<String, dynamic>) {
      return Map<String, Object?>.from(payload);
    }
    throw const FormatException(
      'Authentication response must be a JSON object.',
    );
  }
}
