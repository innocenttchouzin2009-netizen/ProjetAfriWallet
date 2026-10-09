import '../models/auth_session.dart';
import '../network/api_exception.dart';
import 'auth_session_coordinator.dart';

typedef AuthenticatedRequest<T> = Future<T> Function(String accessToken);

class AuthenticatedSessionUnavailableException implements Exception {
  const AuthenticatedSessionUnavailableException();

  @override
  String toString() => 'An authenticated session is required.';
}

class AuthenticatedSessionRequestExecutor {
  const AuthenticatedSessionRequestExecutor(this._sessionLifecycle);

  final AuthSessionLifecycle _sessionLifecycle;

  Future<T> execute<T>(AuthenticatedRequest<T> request) async {
    final session = await _sessionLifecycle.restoreValidSession();
    if (session == null) {
      throw const AuthenticatedSessionUnavailableException();
    }

    try {
      return await request(session.accessToken);
    } on ApiUnauthorizedException {
      final refreshed = await _sessionLifecycle.refreshSession();
      if (refreshed == null) {
        throw const AuthenticatedSessionUnavailableException();
      }

      try {
        return await request(refreshed.accessToken);
      } on ApiUnauthorizedException {
        await _sessionLifecycle.clearLocalSession();
        rethrow;
      }
    }
  }
}
