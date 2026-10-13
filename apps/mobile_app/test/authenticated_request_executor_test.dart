import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/authenticated_request_executor.dart';

void main() {
  group('AuthenticatedRequestExecutor', () {
    test('executes once with the current valid access token', () async {
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('access-current'),
      );
      final executor = AuthenticatedRequestExecutor(lifecycle);

      final tokens = <String>[];
      final result = await executor.execute<String>((accessToken) async {
        tokens.add(accessToken);
        return 'ok';
      });

      expect(result, 'ok');
      expect(tokens, <String>['access-current']);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
    });

    test('throws unauthorized without executing when no session exists', () async {
      final lifecycle = _FakeAuthSessionLifecycle(restoredSession: null);
      final executor = AuthenticatedRequestExecutor(lifecycle);

      var requestCalls = 0;

      await expectLater(
        executor.execute<void>((_) async {
          requestCalls += 1;
        }),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      expect(requestCalls, 0);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
    });

    test('refreshes once and retries once after an unauthorized response', () async {
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('access-old'),
        refreshedSession: _session('access-new'),
      );
      final executor = AuthenticatedRequestExecutor(lifecycle);

      final tokens = <String>[];
      final result = await executor.execute<String>((accessToken) async {
        tokens.add(accessToken);
        if (tokens.length == 1) {
          throw const ApiUnauthorizedException();
        }
        return 'ok';
      });

      expect(result, 'ok');
      expect(tokens, <String>['access-old', 'access-new']);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 1);
    });

    test('does not retry when refresh cannot restore a session', () async {
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('access-old'),
        refreshedSession: null,
      );
      final executor = AuthenticatedRequestExecutor(lifecycle);

      var requestCalls = 0;

      await expectLater(
        executor.execute<void>((_) async {
          requestCalls += 1;
          throw const ApiUnauthorizedException();
        }),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      expect(requestCalls, 1);
      expect(lifecycle.refreshCalls, 1);
    });

    test('propagates a second unauthorized response without another refresh', () async {
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('access-old'),
        refreshedSession: _session('access-new'),
      );
      final executor = AuthenticatedRequestExecutor(lifecycle);

      var requestCalls = 0;

      await expectLater(
        executor.execute<void>((_) async {
          requestCalls += 1;
          throw const ApiUnauthorizedException();
        }),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      expect(requestCalls, 2);
      expect(lifecycle.refreshCalls, 1);
    });

    test('does not refresh for non-unauthorized API failures', () async {
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('access-current'),
      );
      final executor = AuthenticatedRequestExecutor(lifecycle);

      await expectLater(
        executor.execute<void>((_) async {
          throw const ApiForbiddenException();
        }),
        throwsA(isA<ApiForbiddenException>()),
      );

      expect(lifecycle.refreshCalls, 0);
    });
  });
}

StoredAuthSession _session(String accessToken) {
  return StoredAuthSession(
    accessToken: accessToken,
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.utc(2099),
  );
}

class _FakeAuthSessionLifecycle implements AuthSessionLifecycle {
  _FakeAuthSessionLifecycle({
    required this.restoredSession,
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
