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

typedef AuthLogin = Future<StoredAuthSession> Function(AuthLoginRequest request);

@immutable
class AuthState {
  const AuthState._(
    this.status,
    this.session, {
    this.isAuthenticating = false,
    this.authError,
  });

  const AuthState.restoring() : this._(AuthStateStatus.restoring, null);

  const AuthState.unauthenticated()
      : this._(AuthStateStatus.unauthenticated, null);

  const AuthState.authenticating()
      : this._(
          AuthStateStatus.unauthenticated,
          null,
          isAuthenticating: true,
        );

  const AuthState.authenticationFailed(AuthError error)
      : this._(
          AuthStateStatus.unauthenticated,
          null,
          authError: error,
        );

  const AuthState.restorationFailed()
      : this._(AuthStateStatus.restorationFailed, null);

  const AuthState.authenticated(StoredAuthSession session)
      : this._(AuthStateStatus.authenticated, session);

  final AuthStateStatus status;
  final StoredAuthSession? session;
  final bool isAuthenticating;
  final AuthError? authError;

  bool get isAuthenticated => status == AuthStateStatus.authenticated;
}

class AuthStateController extends ChangeNotifier {
  AuthStateController(
    this._sessionLifecycle, {
    AuthLogin? login,
  })
      // The public named parameter intentionally differs from the private field.
      // ignore: prefer_initializing_formals
      : _login = login;

  final AuthSessionLifecycle _sessionLifecycle;
  final AuthLogin? _login;

  AuthState _state = const AuthState.restoring();

  AuthState get state => _state;

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

  Future<void> login(AuthLoginRequest request) async {
    final login = _login;
    if (login == null) {
      throw StateError('Authentication login is not configured.');
    }

    _setState(const AuthState.authenticating());

    try {
      final session = await login(request);
      _setState(AuthState.authenticated(session));
    } on AuthRepositoryException catch (error) {
      _setState(AuthState.authenticationFailed(error.error));
    } catch (_) {
      _setState(const AuthState.unauthenticated());
      rethrow;
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
