import 'package:flutter/foundation.dart';

import '../models/auth_error.dart';
import '../models/auth_requests.dart';
import '../models/auth_session.dart';
import 'auth_repository.dart';
import 'auth_session_coordinator.dart';

enum AuthStateStatus {
  restoring,
  unauthenticated,
  authenticated,
  restorationFailed,
}

enum AuthLoginStatus {
  idle,
  authenticating,
  failed,
}

@immutable
class AuthState {
  const AuthState._(
    this.status,
    this.session, {
    this.loginStatus = AuthLoginStatus.idle,
    this.loginError,
  });

  const AuthState.restoring() : this._(AuthStateStatus.restoring, null);

  const AuthState.unauthenticated()
      : this._(AuthStateStatus.unauthenticated, null);

  const AuthState.restorationFailed()
      : this._(AuthStateStatus.restorationFailed, null);

  const AuthState.authenticated(StoredAuthSession session)
      : this._(AuthStateStatus.authenticated, session);

  const AuthState.authenticating()
      : this._(
          AuthStateStatus.unauthenticated,
          null,
          loginStatus: AuthLoginStatus.authenticating,
        );

  const AuthState.authenticationFailed(AuthError error)
      : this._(
          AuthStateStatus.unauthenticated,
          null,
          loginStatus: AuthLoginStatus.failed,
          loginError: error,
        );

  final AuthStateStatus status;
  final StoredAuthSession? session;
  final AuthLoginStatus loginStatus;
  final AuthError? loginError;

  bool get isAuthenticated => status == AuthStateStatus.authenticated;
  bool get isAuthenticating => loginStatus == AuthLoginStatus.authenticating;
}

class AuthStateController extends ChangeNotifier {
  AuthStateController(this._sessionLifecycle);

  final AuthSessionLifecycle _sessionLifecycle;

  AuthState _state = const AuthState.restoring();

  AuthState get state => _state;

  Future<void> login(AuthLoginRequest request) async {
    _setState(const AuthState.authenticating());

    try {
      final session = await _sessionLifecycle.login(request);
      _setState(AuthState.authenticated(session));
    } on AuthRepositoryException catch (error) {
      _setState(AuthState.authenticationFailed(error.error));
    } catch (_) {
      _setState(
        const AuthState.authenticationFailed(
          AuthError(
            code: AuthErrorCode.unknown,
            message: 'Authentication could not be completed.',
          ),
        ),
      );
    }
  }

  Future<void> restore() async {
    _setState(const AuthState.restoring());

    try {
      final session = await _sessionLifecycle.restoreValidSession();
      if (session == null) {
        _setState(const AuthState.unauthenticated());
        return;
      }

      _setState(AuthState.authenticated(session));
    } catch (_) {
      _setState(const AuthState.restorationFailed());
    }
  }

  Future<void> clearLocalSession() async {
    await _sessionLifecycle.clearLocalSession();
    _setState(const AuthState.unauthenticated());
  }

  void _setState(AuthState next) {
    _state = next;
    notifyListeners();
  }
}
