import '../network/api_exception.dart';
import 'auth_session_coordinator.dart';

typedef AuthenticatedRequest<T> = Future<T> Function(String accessToken);

class AuthenticatedRequestExecutor {
  const AuthenticatedRequestExecutor(this._sessionLifecycle);

  final AuthSessionLifecycle _sessionLifecycle;

  Future<T> execute<T>(AuthenticatedRequest<T> request) async {
    final session = await _sessionLifecycle.restoreValidSession();
    if (session == null) {
      throw const ApiUnauthorizedException();
    }

    try {
      return await request(session.accessToken);
    } on ApiUnauthorizedException {
      final refreshedSession = await _sessionLifecycle.refreshSession();
      if (refreshedSession == null) {
        throw const ApiUnauthorizedException();
      }

      return request(refreshedSession.accessToken);
    }
  }
}
