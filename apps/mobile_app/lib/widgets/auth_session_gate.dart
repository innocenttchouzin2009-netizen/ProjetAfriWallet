import 'dart:async';

import 'package:flutter/widgets.dart';

import '../models/auth_session.dart';
import '../services/auth_state_controller.dart';

typedef AuthSessionGateBuilder = Widget Function(BuildContext context);
typedef AuthenticatedSessionBuilder = Widget Function(
  BuildContext context,
  StoredAuthSession session,
);
typedef AuthRestorationFailedBuilder = Widget Function(
  BuildContext context,
  Future<void> Function() retry,
);

class AuthSessionGate extends StatefulWidget {
  const AuthSessionGate({
    super.key,
    required this.controller,
    required this.restoringBuilder,
    required this.unauthenticatedBuilder,
    required this.authenticatedBuilder,
    required this.restorationFailedBuilder,
  });

  final AuthStateController controller;
  final AuthSessionGateBuilder restoringBuilder;
  final AuthSessionGateBuilder unauthenticatedBuilder;
  final AuthenticatedSessionBuilder authenticatedBuilder;
  final AuthRestorationFailedBuilder restorationFailedBuilder;

  @override
  State<AuthSessionGate> createState() => _AuthSessionGateState();
}

class _AuthSessionGateState extends State<AuthSessionGate> {
  @override
  void initState() {
    super.initState();
    unawaited(widget.controller.restore());
  }

  @override
  void didUpdateWidget(covariant AuthSessionGate oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!identical(oldWidget.controller, widget.controller)) {
      unawaited(widget.controller.restore());
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final state = widget.controller.state;

        switch (state.status) {
          case AuthStateStatus.restoring:
            return widget.restoringBuilder(context);
          case AuthStateStatus.unauthenticated:
            return widget.unauthenticatedBuilder(context);
          case AuthStateStatus.authenticated:
            final session = state.session;
            if (session == null) {
              return widget.restorationFailedBuilder(
                context,
                widget.controller.restore,
              );
            }
            return widget.authenticatedBuilder(context, session);
          case AuthStateStatus.restorationFailed:
            return widget.restorationFailedBuilder(
              context,
              widget.controller.restore,
            );
        }
      },
    );
  }
}
