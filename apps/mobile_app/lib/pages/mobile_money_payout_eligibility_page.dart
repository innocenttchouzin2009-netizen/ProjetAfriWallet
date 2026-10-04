import 'package:flutter/material.dart';

import '../models/mobile_money_payout.dart';
import '../presentation/mobile_money_payout_eligibility_controller.dart';

class MobileMoneyPayoutEligibilityPage extends StatefulWidget {
  const MobileMoneyPayoutEligibilityPage({
    super.key,
    required this.controller,
    required this.sourceCountryCode,
    required this.payout,
    this.onEligibleContinue,
    this.onBack,
  });

  final MobileMoneyPayoutEligibilityController controller;
  final String sourceCountryCode;
  final MobileMoneyPayoutRequest payout;
  final VoidCallback? onEligibleContinue;
  final VoidCallback? onBack;

  @override
  State<MobileMoneyPayoutEligibilityPage> createState() =>
      _MobileMoneyPayoutEligibilityPageState();
}

class _MobileMoneyPayoutEligibilityPageState
    extends State<MobileMoneyPayoutEligibilityPage> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerChanged);
    WidgetsBinding.instance.addPostFrameCallback((_) => _checkEligibility());
  }

  @override
  void didUpdateWidget(covariant MobileMoneyPayoutEligibilityPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.controller != widget.controller) {
      oldWidget.controller.removeListener(_onControllerChanged);
      widget.controller.addListener(_onControllerChanged);
    }
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerChanged);
    super.dispose();
  }

  void _onControllerChanged() {
    if (mounted) {
      setState(() {});
    }
  }

  Future<void> _checkEligibility() {
    return widget.controller.checkEligibility(
      sourceCountryCode: widget.sourceCountryCode,
      payout: widget.payout,
    );
  }

  @override
  Widget build(BuildContext context) {
    final status = widget.controller.status;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Mobile Money'),
        leading: widget.onBack == null
            ? null
            : IconButton(
                key: const Key('momo-eligibility-back'),
                onPressed: widget.onBack,
                icon: const Icon(Icons.arrow_back),
              ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: switch (status) {
            MobileMoneyPayoutEligibilityPresentationStatus.idle ||
            MobileMoneyPayoutEligibilityPresentationStatus.checking =>
              const _CheckingView(),
            MobileMoneyPayoutEligibilityPresentationStatus.eligible =>
              _EligibleView(onContinue: widget.onEligibleContinue),
            MobileMoneyPayoutEligibilityPresentationStatus.ineligible =>
              _IneligibleView(
                failureCode: widget.controller.failureCode,
                onRetry: _checkEligibility,
              ),
            MobileMoneyPayoutEligibilityPresentationStatus.failed =>
              _FailedView(onRetry: _checkEligibility),
          },
        ),
      ),
    );
  }
}

class _CheckingView extends StatelessWidget {
  const _CheckingView();

  @override
  Widget build(BuildContext context) {
    return const Center(
      key: Key('momo-eligibility-checking'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircularProgressIndicator(),
          SizedBox(height: 16),
          Text(
            'Vérification de la disponibilité du transfert Mobile Money…',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

class _EligibleView extends StatelessWidget {
  const _EligibleView({required this.onContinue});

  final VoidCallback? onContinue;

  @override
  Widget build(BuildContext context) {
    return Center(
      key: const Key('momo-eligibility-eligible'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.check_circle_outline, size: 56),
          const SizedBox(height: 16),
          Text(
            'Transfert disponible',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'Ce corridor Mobile Money est disponible. Aucun transfert n’a encore été lancé.',
            textAlign: TextAlign.center,
          ),
          if (onContinue != null) ...[
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('momo-eligibility-continue'),
              onPressed: onContinue,
              child: const Text('Continuer'),
            ),
          ],
        ],
      ),
    );
  }
}

class _IneligibleView extends StatelessWidget {
  const _IneligibleView({
    required this.failureCode,
    required this.onRetry,
  });

  final String? failureCode;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      key: const Key('momo-eligibility-ineligible'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.info_outline, size: 56),
          const SizedBox(height: 16),
          Text(
            'Transfert indisponible',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'Ce transfert Mobile Money n’est pas disponible pour cette destination ou cet opérateur.',
            textAlign: TextAlign.center,
          ),
          if (failureCode != null && failureCode!.trim().isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(
              'Code : $failureCode',
              key: const Key('momo-eligibility-failure-code'),
            ),
          ],
          const SizedBox(height: 24),
          OutlinedButton(
            key: const Key('momo-eligibility-retry-ineligible'),
            onPressed: onRetry,
            child: const Text('Réessayer'),
          ),
        ],
      ),
    );
  }
}

class _FailedView extends StatelessWidget {
  const _FailedView({required this.onRetry});

  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      key: const Key('momo-eligibility-failed'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off_outlined, size: 56),
          const SizedBox(height: 16),
          Text(
            'Vérification impossible',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'La disponibilité du transfert n’a pas pu être vérifiée. Aucun transfert n’a été lancé.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          OutlinedButton(
            key: const Key('momo-eligibility-retry-failed'),
            onPressed: onRetry,
            child: const Text('Réessayer'),
          ),
        ],
      ),
    );
  }
}
