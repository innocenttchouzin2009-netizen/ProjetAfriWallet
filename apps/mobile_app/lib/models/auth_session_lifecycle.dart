import 'auth_session.dart';

enum AuthSessionLifecycle {
  active,
  refreshRequired,
}

extension StoredAuthSessionLifecycle on StoredAuthSession {
  AuthSessionLifecycle lifecycleAt(DateTime nowUtc) {
    return isAccessTokenExpired(nowUtc)
        ? AuthSessionLifecycle.refreshRequired
        : AuthSessionLifecycle.active;
  }
}
