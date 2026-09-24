import '../models/auth_error.dart';
import '../models/auth_requests.dart';
import '../models/auth_session.dart';
import '../models/current_auth_session.dart';
import '../network/api_exception.dart';
import '../network/auth_error_parser.dart';
import 'auth_remote_data_source.dart';
import 'secure_session_store.dart';

abstract interface class AuthRepository {
  Future<StoredAuthSession> login(AuthLoginRequest request);
  Future<StoredAuthSession> refresh();
  Future<StoredAuthSession?> readStoredSession();
  Future<CurrentAuthSession> loadCurrentSession();
  Future<void> logout();
  Future<void> logoutAll();
}

class AuthRepositoryException implements Exception {
  const AuthRepositoryException(this.error);

  final AuthError error;

  @override
  String toString() => error.message;
}

class RemoteAuthRepository implements AuthRepository {
  RemoteAuthRepository(
    this._remoteDataSource,
    this._sessionStore, {
    AuthErrorParser errorParser = const AuthErrorParser(),
    DateTime Function()? utcNow,
  })  : _errorParser = errorParser,
        _utcNow = utcNow ?? DateTime.now;

  final AuthRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;
  final AuthErrorParser _errorParser;
  final DateTime Function() _utcNow;

  @override
  Future<StoredAuthSession> login(AuthLoginRequest request) {
    return _guard(() async {
      final response = await _remoteDataSource.login(request);
      final session = response.toStored(receivedAtUtc: _utcNow().toUtc());
      await _sessionStore.save(session);
      return session;
    });
  }

  @override
  Future<StoredAuthSession> refresh() {
    return _guard(() async {
      final current = await _requireStoredSession();
      final response = await _remoteDataSource.refresh(
        AuthRefreshRequest(refreshToken: current.refreshToken),
      );
      final refreshed = response.toStored(receivedAtUtc: _utcNow().toUtc());
      await _sessionStore.save(refreshed);
      return refreshed;
    });
  }

  @override
  Future<StoredAuthSession?> readStoredSession() => _sessionStore.read();

  @override
  Future<CurrentAuthSession> loadCurrentSession() {
    return _guard(() async {
      final current = await _requireStoredSession();
      return _remoteDataSource.loadCurrentSession(current.accessToken);
    });
  }

  @override
  Future<void> logout() async {
    final current = await _sessionStore.read();
    if (current == null) {
      await _sessionStore.clear();
      return;
    }

    try {
      await _guard(() => _remoteDataSource.logout(current.accessToken));
    } finally {
      await _sessionStore.clear();
    }
  }

  @override
  Future<void> logoutAll() async {
    final current = await _sessionStore.read();
    if (current == null) {
      await _sessionStore.clear();
      return;
    }

    try {
      await _guard(() => _remoteDataSource.logoutAll(current.accessToken));
    } finally {
      await _sessionStore.clear();
    }
  }

  Future<StoredAuthSession> _requireStoredSession() async {
    final session = await _sessionStore.read();
    if (session != null) {
      return session;
    }

    throw const AuthRepositoryException(
      AuthError(
        code: AuthErrorCode.sessionExpired,
        message: 'No stored authentication session is available.',
      ),
    );
  }

  Future<T> _guard<T>(Future<T> Function() operation) async {
    try {
      return await operation();
    } on AuthRepositoryException {
      rethrow;
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
