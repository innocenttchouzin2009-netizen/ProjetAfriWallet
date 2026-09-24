import '../models/auth_error.dart';
import '../models/auth_requests.dart';
import '../models/auth_session.dart';
import '../models/current_auth_session.dart';
import '../network/api_exception.dart';
import '../network/auth_error_parser.dart';
import 'auth_remote_data_source.dart';

abstract interface class AuthRepository {
  Future<AuthSession> login(AuthLoginRequest request);
  Future<AuthSession> refresh(AuthRefreshRequest request);
  Future<void> logout(String accessToken);
  Future<void> logoutAll(String accessToken);
  Future<CurrentAuthSession> loadCurrentSession(String accessToken);
}

class AuthRepositoryException implements Exception {
  const AuthRepositoryException(this.error);

  final AuthError error;

  @override
  String toString() => error.message;
}

class RemoteAuthRepository implements AuthRepository {
  RemoteAuthRepository(
    this._remoteDataSource, {
    AuthErrorParser errorParser = const AuthErrorParser(),
  }) : _errorParser = errorParser;

  final AuthRemoteDataSource _remoteDataSource;
  final AuthErrorParser _errorParser;

  @override
  Future<AuthSession> login(AuthLoginRequest request) =>
      _guard(() => _remoteDataSource.login(request));

  @override
  Future<AuthSession> refresh(AuthRefreshRequest request) =>
      _guard(() => _remoteDataSource.refresh(request));

  @override
  Future<void> logout(String accessToken) =>
      _guard(() => _remoteDataSource.logout(accessToken));

  @override
  Future<void> logoutAll(String accessToken) =>
      _guard(() => _remoteDataSource.logoutAll(accessToken));

  @override
  Future<CurrentAuthSession> loadCurrentSession(String accessToken) =>
      _guard(() => _remoteDataSource.loadCurrentSession(accessToken));

  Future<T> _guard<T>(Future<T> Function() operation) async {
    try {
      return await operation();
    } on ApiException catch (error) {
      throw AuthRepositoryException(_errorParser.parse(error));
    } on FormatException {
      throw const AuthRepositoryException(
        AuthError(
          code: AuthErrorCode.unknown,
          message: 'The authentication service returned an invalid response.',
        ),
      );
    }
  }
}
