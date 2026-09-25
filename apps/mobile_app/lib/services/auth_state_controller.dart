import 'package:flutter/foundation.dart';

import '../models/auth_session.dart';
import 'auth_session_store.dart';

enum AuthStateStatus {
  restoring,
  unauthenticated,
  authenticated,
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

  final AuthStateStatus status;
  final StoredAuthSession? session;

  bool get isAuthenticated => status == AuthStateStatus.authenticated;
}

class AuthStateController extends ChangeNotifier {
  AuthStateController(this._sessionStore);

  final SecureSessionStore _sessionStore;

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

      _setState(AuthState.authenticated(session));
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
