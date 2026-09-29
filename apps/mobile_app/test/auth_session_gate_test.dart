import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/auth_state_controller.dart';
import 'package:mobile_app/widgets/auth_session_gate.dart';

void main() {
  testWidgets('starts restoration exactly once and renders restoring state',
      (tester) async {
    final completer = Completer<StoredAuthSession?>();
    final lifecycle = _FakeSessionLifecycle(
      restoreHandler: () => completer.future,
    );
    final controller = AuthStateController(lifecycle);
    addTearDown(controller.dispose);

    await tester.pumpWidget(_app(controller));

    expect(lifecycle.restoreCalls, 1);
    expect(find.text('restoring'), findsOneWidget);

    completer.complete(null);
    await tester.pump();

    expect(find.text('unauthenticated'), findsOneWidget);
  });

  testWidgets('renders authenticated branch with restored session',
      (tester) async {
    final lifecycle = _FakeSessionLifecycle(
      restoreHandler: () async => _session(),
    );
    final controller = AuthStateController(lifecycle);
    addTearDown(controller.dispose);

    await tester.pumpWidget(_app(controller));
    await tester.pump();

    expect(find.text('authenticated:user-1'), findsOneWidget);
    expect(lifecycle.restoreCalls, 1);
  });

  testWidgets('renders unauthenticated branch when no session is restored',
      (tester) async {
    final lifecycle = _FakeSessionLifecycle(
      restoreHandler: () async => null,
    );
    final controller = AuthStateController(lifecycle);
    addTearDown(controller.dispose);

    await tester.pumpWidget(_app(controller));
    await tester.pump();

    expect(find.text('unauthenticated'), findsOneWidget);
    expect(lifecycle.restoreCalls, 1);
  });

  testWidgets('fails closed when session restoration fails', (tester) async {
    final lifecycle = _FakeSessionLifecycle(
      restoreHandler: () async => throw StateError('restore unavailable'),
    );
    final controller = AuthStateController(lifecycle);
    addTearDown(controller.dispose);

    await tester.pumpWidget(_app(controller));
    await tester.pump();

    expect(find.text('restoration-failed'), findsOneWidget);
    expect(find.text('retry'), findsOneWidget);
    expect(controller.state.isAuthenticated, isFalse);
  });

  testWidgets('retry requests a new restoration attempt', (tester) async {
    var shouldFail = true;
    final lifecycle = _FakeSessionLifecycle(
      restoreHandler: () async {
        if (shouldFail) {
          throw StateError('first restore unavailable');
        }
        return _session();
      },
    );
    final controller = AuthStateController(lifecycle);
    addTearDown(controller.dispose);

    await tester.pumpWidget(_app(controller));
    await tester.pump();

    expect(find.text('restoration-failed'), findsOneWidget);
    expect(lifecycle.restoreCalls, 1);

    shouldFail = false;
    await tester.tap(find.text('retry'));
    await tester.pump();

    expect(lifecycle.restoreCalls, 2);
    expect(find.text('authenticated:user-1'), findsOneWidget);
  });
}

Widget _app(AuthStateController controller) {
  return MaterialApp(
    home: AuthSessionGate(
      controller: controller,
      restoringBuilder: (_) => const Text('restoring'),
      unauthenticatedBuilder: (_) => const Text('unauthenticated'),
      authenticatedBuilder: (_, session) =>
          Text('authenticated:${session.userId}'),
      restorationFailedBuilder: (_, retry) => Column(
        children: <Widget>[
          const Text('restoration-failed'),
          TextButton(
            onPressed: retry,
            child: const Text('retry'),
          ),
        ],
      ),
    ),
  );
}

StoredAuthSession _session() {
  return StoredAuthSession(
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 29, 14),
  );
}

class _FakeSessionLifecycle implements AuthSessionLifecycle {
  _FakeSessionLifecycle({
    required this.restoreHandler,
  });

  final Future<StoredAuthSession?> Function() restoreHandler;
  int restoreCalls = 0;

  @override
  Future<StoredAuthSession?> restoreValidSession() {
    restoreCalls += 1;
    return restoreHandler();
  }

  @override
  Future<StoredAuthSession?> refreshSession() => restoreHandler();

  @override
  Future<void> clearLocalSession() async {}
}
