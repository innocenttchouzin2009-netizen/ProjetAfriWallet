import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../models/mobile_money_payout_execution_handoff.dart';
import '../models/mobile_money_payout_execution_preparation.dart';
import '../presentation/mobile_money_payout_execution_preparation_controller.dart';

class MobileMoneyPayoutExecutionPreparationPage extends StatefulWidget {
  const MobileMoneyPayoutExecutionPreparationPage({
    super.key,
    required this.controller,
    required this.fundingCorrelationId,
    required this.idempotencyKey,
    required this.handoff,
    this.onBack,
  });

  final MobileMoneyPayoutExecutionPreparationController controller;
  final String fundingCorrelationId;
  final String idempotencyKey;
  final MobileMoneyPayoutExecutionHandoff handoff;
  final VoidCallback? onBack;

  @override
  State<MobileMoneyPayoutExecutionPreparationPage> createState() =>
      _MobileMoneyPayoutExecutionPreparationPageState();
}

class _MobileMoneyPayoutExecutionPreparationPageState
    extends State<MobileMoneyPayoutExecutionPreparationPage> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerChanged);
    WidgetsBinding.instance.addPostFrameCallback((_) => _prepare());
  }

  @override
  void didUpdateWidget(
    covariant MobileMoneyPayoutExecutionPreparationPage oldWidget,
  ) {
    super.didUpdateWidget(oldWidget);
    final controllerChanged = oldWidget.controller != widget.controller;
    final requestChanged =
        oldWidget.fundingCorrelationId != widget.fundingCorrelationId ||
            oldWidget.idempotencyKey != widget.idempotencyKey ||
            oldWidget.handoff != widget.handoff;

    if (controllerChanged) {
      oldWidget.controller.removeListener(_onControllerChanged);
      widget.controller.addListener(_onControllerChanged);
    }

    if (controllerChanged || requestChanged) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _prepare());
    }
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerChanged);
    super.dispose();
  }

  void _onControllerChanged() {
    if (mounted) setState(() {});
  }

  Future<void> _prepare() {
    return widget.controller.prepareExecution(
      fundingCorrelationId: widget.fundingCorrelationId,
      idempotencyKey: widget.idempotencyKey,
      handoff: widget.handoff,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      key: const Key('momo-execution-preparation-page'),
      appBar: AppBar(
        title: const Text('Préparation du transfert'),
        leading: widget.onBack == null
            ? null
            : IconButton(
                key: const Key('momo-execution-preparation-back'),
                onPressed: widget.onBack,
                icon: const Icon(Icons.arrow_back),
              ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: _buildBody(context),
        ),
      ),
    );
  }

  Widget _buildBody(BuildContext context) {
    switch (widget.controller.status) {
      case MobileMoneyPayoutExecutionPreparationPresentationStatus.idle:
      case MobileMoneyPayoutExecutionPreparationPresentationStatus.loading:
        return const _LoadingView();
      case MobileMoneyPayoutExecutionPreparationPresentationStatus.ready:
        final preparation = widget.controller.preparation;
        if (preparation == null) {
          return _FailedView(onRetry: _prepare);
        }
        return _ReadyView(preparation: preparation);
      case MobileMoneyPayoutExecutionPreparationPresentationStatus.failed:
        return _FailedView(onRetry: _prepare);
    }
  }
}

class _LoadingView extends StatelessWidget {
  const _LoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(
      key: Key('momo-execution-preparation-loading'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircularProgressIndicator(),
          SizedBox(height: 16),
          Text(
            'Préparation sécurisée du transfert…',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

class _ReadyView extends StatelessWidget {
  const _ReadyView({required this.preparation});

  final MobileMoneyPayoutExecutionPreparation preparation;

  @override
  Widget build(BuildContext context) {
    return ListView(
      key: const Key('momo-execution-preparation-ready'),
      children: [
        Text(
          'Exécution préparée',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 8),
        const Text(
          'AfrikaWallet a lié le devis et le plan de paiement de façon idempotente. Le transfert n’a pas encore été soumis à un opérateur.',
        ),
        const SizedBox(height: 20),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Statut'),
                const SizedBox(height: 6),
                Text(
                  preparation.status,
                  key: const Key('momo-execution-preparation-status'),
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 12),
                const Text('Binding'),
                const SizedBox(height: 6),
                Text(
                  preparation.bindingId,
                  key: const Key('momo-execution-preparation-binding-id'),
                ),
                const SizedBox(height: 12),
                Text(
                  preparation.isReplay
                      ? 'Relecture idempotente confirmée'
                      : 'Nouvelle préparation confirmée',
                  key: const Key('momo-execution-preparation-replay'),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Text(
          'Préparé le ${DateFormat('dd/MM/yyyy HH:mm').format(preparation.preparedAtUtc.toLocal())}.',
          key: const Key('momo-execution-preparation-prepared-at'),
        ),
        const SizedBox(height: 12),
        const Text(
          'Aucun débit, aucune réservation et aucun transfert Mobile Money n’a encore été lancé.',
          key: Key('momo-execution-preparation-no-payout'),
        ),
      ],
    );
  }
}

class _FailedView extends StatelessWidget {
  const _FailedView({required this.onRetry});

  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      key: const Key('momo-execution-preparation-failed'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off_outlined, size: 56),
          const SizedBox(height: 16),
          Text(
            'Préparation indisponible',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'Le binding d’exécution n’a pas pu être préparé. Aucun débit ni transfert n’a été lancé.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          OutlinedButton(
            key: const Key('momo-execution-preparation-retry'),
            onPressed: onRetry,
            child: const Text('Réessayer'),
          ),
        ],
      ),
    );
  }
}
