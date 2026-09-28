import '../models/auth_error.dart';
import '../models/auth_session.dart';
import 'auth_repository.dart';
import 'secure_session_store.dart';

abstract interface class AuthSessionLifecycle {
  Future<StoredAuthSession?> restoreValidSession();
  Future<StoredAuthSession?> refreshSession();
  Future<void> clearLocalSession();
}

class AuthSessionCoordinator implements AuthSessionLifecycle {
  AuthSessionCoordinator(
    this._repository,
    this._sessionStore, {
    DateTime Function()? utcNow,
  }) : _utcNow = utcNow ?? DateTime.now;

  final AuthRepository _repository;
  final AuthSessionStore _sessionStore;
  final DateTime Function() _utcNow;

  Future<StoredAuthSession?>? _refreshInFlight;

  @override
  Future<StoredAuthSession?> restoreValidSession() async {
    final session = await _sessionStore.read();
    if (session == null) {
      return null;
    }

    if (!session.isAccessTokenExpired(_utcNow().toUtc())) {
      return session;
    }

    return _refreshSingleFlight();
  }

  @override
  Future<StoredAuthSession?> refreshSession() async {
    final session = await _sessionStore.read();
    if (session == null) {
      return null;
    }

    return _refreshSingleFlight();
  }

  @override
  Future<void> clearLocalSession() => _sessionStore.clear();

  Future<StoredAuthSession?> _refreshSingleFlight() {
    final activeRefresh = _refreshInFlight;
    if (activeRefresh != null) {
      return activeRefresh;
    }

    late final Future<StoredAuthSession?> operation;
    operation = _refreshExpiredSession().whenComplete(() {
      if (identical(_refreshInFlight, operation)) {
        _refreshInFlight = null;
      }
    });
    _refreshInFlight = operation;
    return operation;
  }

  Future<StoredAuthSession?> _refreshExpiredSession() async {
    try {
      return await _repository.refresh();
    } on AuthRepositoryException catch (error) {
      if (_requiresLocalInvalidation(error.error.code)) {
        await _sessionStore.clear();
        return null;
      }
      rethrow;
    }
  }

  bool _requiresLocalInvalidation(AuthErrorCode code) {
    switch (code) {
      case AuthErrorCode.sessionExpired:
      case AuthErrorCode.sessionRevoked:
      case AuthErrorCode.refreshInvalid:
      case AuthErrorCode.refreshExpired:
      case AuthErrorCode.refreshReused:
      case AuthErrorCode.tokenInvalid:
      case AuthErrorCode.tokenExpired:
      case AuthErrorCode.userDisabled:
        return true;
      case AuthErrorCode.invalidCredentials:
      case AuthErrorCode.identifierAlreadyExists:
      case AuthErrorCode.validationError:
      case AuthErrorCode.unknown:
        return false;
    }
  }
}
