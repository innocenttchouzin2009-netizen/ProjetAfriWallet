import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/current_auth_session.dart';
import 'package:mobile_app/services/auth_repository.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/secure_session_store.dart';

void main() {
  group('AuthSessionCoordinator', () {
    test('reuses a locally valid access token without refreshing', () async {
      final store = _MemorySessionStore()
        ..value = _session(
          accessToken: 'access-current',
          expiresAtUtc: DateTime.parse('2026-09-28T10:30:00Z'),
        );
      final repository = _FakeAuthRepository();
      final coordinator = AuthSessionCoordinator(
        repository,
        store,
        utcNow: () => DateTime.parse('2026-09-28T10:00:00Z'),
      );

      final resolved = await coordinator.restoreValidSession();

      expect(resolved?.accessToken, 'access-current');
      expect(repository.refreshCalls, 0);
    });

    test('refreshes an expired access token before restoring auth state',
        () async {
      final store = _MemorySessionStore()
        ..value = _session(
          accessToken: 'access-expired',
          expiresAtUtc: DateTime.parse('2026-09-28T09:59:59Z'),
        );
      final refreshed = _session(
        accessToken: 'access-new',
        refreshToken: 'refresh-new',
        expiresAtUtc: DateTime.parse('2026-09-28T10:15:00Z'),
      );
      final repository = _FakeAuthRepository()
        ..refreshHandler = () async {
          store.value = refreshed;
          return refreshed;
        };
      final coordinator = AuthSessionCoordinator(
        repository,
        store,
        utcNow: () => DateTime.parse('2026-09-28T10:00:00Z'),
      );

      final resolved = await coordinator.restoreValidSession();

      expect(resolved?.accessToken, 'access-new');
      expect(resolved?.refreshToken, 'refresh-new');
      expect(repository.refreshCalls, 1);
    });

    test('coalesces concurrent refresh requests into one remote refresh',
        () async {
      final store = _MemorySessionStore()
        ..value = _session(
          expiresAtUtc: DateTime.parse('2026-09-28T09:00:00Z'),
        );
      final completer = Completer<StoredAuthSession>();
      final repository = _FakeAuthRepository()
        ..refreshHandler = () => completer.future;
      final coordinator = AuthSessionCoordinator(
        repository,
        store,
        utcNow: () => DateTime.parse('2026-09-28T10:00:00Z'),
      );

      final first = coordinator.restoreValidSession();
      final second = coordinator.restoreValidSession();

      await Future<void>.delayed(Duration.zero);
      expect(repository.refreshCalls, 1);

      final refreshed = _session(
        accessToken: 'access-new',
        refreshToken: 'refresh-new',
        expiresAtUtc: DateTime.parse('2026-09-28T10:15:00Z'),
      );
      store.value = refreshed;
      completer.complete(refreshed);

      expect((await first)?.accessToken, 'access-new');
      expect((await second)?.accessToken, 'access-new');
      expect(repository.refreshCalls, 1);
    });

    test('clears local session when refresh token reuse is reported', () async {
      final store = _MemorySessionStore()..value = _session();
      final repository = _FakeAuthRepository()
        ..refreshHandler = () async {
          throw const AuthRepositoryException(
            AuthError(
              code: AuthErrorCode.refreshReused,
              message: 'Refresh token reuse detected.',
            ),
          );
        };
      final coordinator = AuthSessionCoordinator(repository, store);

      final resolved = await coordinator.refreshSession();

      expect(resolved, isNull);
      expect(store.value, isNull);
      expect(repository.refreshCalls, 1);
    });

    test('keeps stored refresh material on retryable refresh failure', () async {
      final original = _session();
      final store = _MemorySessionStore()..value = original;
      final repository = _FakeAuthRepository()
        ..refreshHandler = () async {
          throw const AuthRepositoryException(
            AuthError(
              code: AuthErrorCode.unknown,
              message: 'Temporary authentication service failure.',
            ),
          );
        };
      final coordinator = AuthSessionCoordinator(repository, store);

      await expectLater(
        coordinator.refreshSession(),
        throwsA(isA<AuthRepositoryException>()),
      );

      expect(store.value, same(original));
    });

    test('returns unauthenticated without attempting refresh when empty',
        () async {
      final repository = _FakeAuthRepository();
      final coordinator = AuthSessionCoordinator(
        repository,
        _MemorySessionStore(),
      );

      expect(await coordinator.restoreValidSession(), isNull);
      expect(await coordinator.refreshSession(), isNull);
      expect(repository.refreshCalls, 0);
    });
  });
}

StoredAuthSession _session({
  String accessToken = 'access-token',
  String refreshToken = 'refresh-token',
  DateTime? expiresAtUtc,
}) {
  return StoredAuthSession(
    accessToken: accessToken,
    refreshToken: refreshToken,
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc:
        expiresAtUtc ?? DateTime.parse('2026-09-28T10:15:00Z'),
  );
}

class _MemorySessionStore implements AuthSessionStore {
  StoredAuthSession? value;

  @override
  Future<void> save(StoredAuthSession session) async {
    value = session;
  }

  @override
  Future<StoredAuthSession?> read() async => value;

  @override
  Future<void> clear() async {
    value = null;
  }
}

class _FakeAuthRepository implements AuthRepository {
  int refreshCalls = 0;
  Future<StoredAuthSession> Function()? refreshHandler;

  @override
  Future<StoredAuthSession> refresh() {
    refreshCalls += 1;
    final handler = refreshHandler;
    if (handler == null) {
      throw StateError('Unexpected refresh call.');
    }
    return handler();
  }

  @override
  Future<StoredAuthSession> login(AuthLoginRequest request) =>
      throw UnimplementedError();

  @override
  Future<StoredAuthSession?> readStoredSession() =>
      throw UnimplementedError();

  @override
  Future<CurrentAuthSession> loadCurrentSession() =>
      throw UnimplementedError();

  @override
  Future<void> logout() => throw UnimplementedError();

  @override
  Future<void> logoutAll() => throw UnimplementedError();
}
