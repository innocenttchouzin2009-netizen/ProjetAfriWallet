import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/authenticated_session_request_executor.dart';

void main() {
  group('AuthenticatedSessionRequestExecutor', () {
    test('executes once with the restored access token', () async {
      final lifecycle = _FakeSessionLifecycle(
        restoredSession: _session(accessToken: 'access-current'),
      );
      final executor = AuthenticatedSessionRequestExecutor(lifecycle);
      final tokens = <String>[];

      final result = await executor.execute((accessToken) async {
        tokens.add(accessToken);
        return 'ok';
      });

      expect(result, 'ok');
      expect(tokens, <String>['access-current']);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
      expect(lifecycle.clearCalls, 0);
    });

    test('rejects execution when no authenticated session is available',
        () async {
      final lifecycle = _FakeSessionLifecycle();
      final executor = AuthenticatedSessionRequestExecutor(lifecycle);
      var requestCalls = 0;

      await expectLater(
        executor.execute((_) async {
          requestCalls += 1;
          return 'unexpected';
        }),
        throwsA(isA<AuthenticatedSessionUnavailableException>()),
      );

      expect(requestCalls, 0);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
    });

    test('refreshes once after 401 and retries with the refreshed token',
        () async {
      final lifecycle = _FakeSessionLifecycle(
        restoredSession: _session(accessToken: 'access-old'),
        refreshedSession: _session(accessToken: 'access-new'),
      );
      final executor = AuthenticatedSessionRequestExecutor(lifecycle);
      final tokens = <String>[];

      final result = await executor.execute((accessToken) async {
        tokens.add(accessToken);
        if (accessToken == 'access-old') {
          throw const ApiUnauthorizedException();
        }
        return 'ok';
      });

      expect(result, 'ok');
      expect(tokens, <String>['access-old', 'access-new']);
      expect(lifecycle.refreshCalls, 1);
      expect(lifecycle.clearCalls, 0);
    });

    test('fails closed when refresh cannot restore a session', () async {
      final lifecycle = _FakeSessionLifecycle(
        restoredSession: _session(accessToken: 'access-old'),
      );
      final executor = AuthenticatedSessionRequestExecutor(lifecycle);
      var requestCalls = 0;

      await expectLater(
        executor.execute((_) async {
          requestCalls += 1;
          throw const ApiUnauthorizedException();
        }),
        throwsA(isA<AuthenticatedSessionUnavailableException>()),
      );

      expect(requestCalls, 1);
      expect(lifecycle.refreshCalls, 1);
      expect(lifecycle.clearCalls, 0);
    });

    test('never loops refresh and clears session after retry is also 401',
        () async {
      final lifecycle = _FakeSessionLifecycle(
        restoredSession: _session(accessToken: 'access-old'),
        refreshedSession: _session(accessToken: 'access-new'),
      );
      final executor = AuthenticatedSessionRequestExecutor(lifecycle);
      final tokens = <String>[];

      await expectLater(
        executor.execute((accessToken) async {
          tokens.add(accessToken);
          throw const ApiUnauthorizedException();
        }),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      expect(tokens, <String>['access-old', 'access-new']);
      expect(lifecycle.refreshCalls, 1);
      expect(lifecycle.clearCalls, 1);
    });

    test('does not refresh non-401 API failures', () async {
      final lifecycle = _FakeSessionLifecycle(
        restoredSession: _session(accessToken: 'access-current'),
      );
      final executor = AuthenticatedSessionRequestExecutor(lifecycle);

      await expectLater(
        executor.execute((_) async {
          throw const ApiServerException(statusCode: 503);
        }),
        throwsA(isA<ApiServerException>()),
      );

      expect(lifecycle.refreshCalls, 0);
      expect(lifecycle.clearCalls, 0);
    });
  });
}

StoredAuthSession _session({required String accessToken}) {
  return StoredAuthSession(
    accessToken: accessToken,
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.parse('2026-10-10T12:00:00Z'),
  );
}

class _FakeSessionLifecycle implements AuthSessionLifecycle {
  _FakeSessionLifecycle({
    this.restoredSession,
    this.refreshedSession,
  });

  final StoredAuthSession? restoredSession;
  final StoredAuthSession? refreshedSession;

  int restoreCalls = 0;
  int refreshCalls = 0;
  int clearCalls = 0;

  @override
  Future<StoredAuthSession?> restoreValidSession() async {
    restoreCalls += 1;
    return restoredSession;
  }

  @override
  Future<StoredAuthSession?> refreshSession() async {
    refreshCalls += 1;
    return refreshedSession;
  }

  @override
  Future<void> clearLocalSession() async {
    clearCalls += 1;
  }
}
