import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/auth_session_lifecycle.dart';

void main() {
  test('active session remains active before access-token expiry', () {
    final session = _session(DateTime.utc(2026, 9, 28, 10, 15));

    expect(
      session.lifecycleAt(DateTime.utc(2026, 9, 28, 10, 14, 59)),
      AuthSessionLifecycle.active,
    );
  });

  test('session requires refresh exactly at access-token expiry', () {
    final expiry = DateTime.utc(2026, 9, 28, 10, 15);
    final session = _session(expiry);

    expect(
      session.lifecycleAt(expiry),
      AuthSessionLifecycle.refreshRequired,
    );
  });

  test('session requires refresh after access-token expiry', () {
    final session = _session(DateTime.utc(2026, 9, 28, 10, 15));

    expect(
      session.lifecycleAt(DateTime.utc(2026, 9, 28, 10, 16)),
      AuthSessionLifecycle.refreshRequired,
    );
  });
}

StoredAuthSession _session(DateTime expiresAtUtc) {
  return StoredAuthSession(
    accessToken: 'access-secret',
    refreshToken: 'refresh-secret',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: expiresAtUtc,
  );
}
