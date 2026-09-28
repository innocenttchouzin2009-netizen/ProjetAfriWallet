import 'package:flutter/foundation.dart';

import '../models/auth_session.dart';
import '../models/auth_session_lifecycle.dart';
import 'secure_session_store.dart';

enum AuthStateStatus {
  restoring,
  unauthenticated,
  authenticated,
  refreshRequired,
  restorationFailed,
}

@immutable
class AuthState {
  const AuthState._(this.status, this.session);

  const AuthState.restoring() : this._(AuthStateStatus.restoring, null);

  const AuthState.unauthenticated()
      : this._(AuthStateStatus.unauthenticated, null);

  const AuthState.restorationFailed()
      : this._(AuthStateStatus.restorationFailed, null);

  const AuthState.authenticated(StoredAuthSession session)
      : this._(AuthStateStatus.authenticated, session);

  const AuthState.refreshRequired(StoredAuthSession session)
      : this._(AuthStateStatus.refreshRequired, session);

  final AuthStateStatus status;
  final StoredAuthSession? session;

  bool get isAuthenticated => status == AuthStateStatus.authenticated;

  bool get hasStoredSession => session != null;
}

class AuthStateController extends ChangeNotifier {
  AuthStateController(
    this._sessionStore, {
    DateTime Function()? utcNow,
  }) : _utcNow = utcNow ?? DateTime.now;

  final AuthSessionStore _sessionStore;
  final DateTime Function() _utcNow;

  AuthState _state = const AuthState.restoring();

  AuthState get state => _state;

  Future<void> restore() async {
    _setState(const AuthState.restoring());

    try {
      final session = await _sessionStore.read();
      if (session == null) {
        _setState(const AuthState.unauthenticated());
        return;
      }

      switch (session.lifecycleAt(_utcNow())) {
        case AuthSessionLifecycle.active:
          _setState(AuthState.authenticated(session));
          return;
        case AuthSessionLifecycle.refreshRequired:
          _setState(AuthState.refreshRequired(session));
          return;
      }
    } catch (_) {
      _setState(const AuthState.restorationFailed());
    }
  }

  Future<void> clearLocalSession() async {
    await _sessionStore.clear();
    _setState(const AuthState.unauthenticated());
  }

  void _setState(AuthState next) {
    _state = next;
    notifyListeners();
  }
}
