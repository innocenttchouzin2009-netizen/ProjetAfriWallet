import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../models/mobile_money_payout.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_funding_plan.dart';
import '../presentation/mobile_money_payout_funding_planning_controller.dart';

class MobileMoneyPayoutFundingPlanningPage extends StatefulWidget {
  const MobileMoneyPayoutFundingPlanningPage({
    super.key,
    required this.controller,
    required this.correlationId,
    required this.intent,
    required this.requestedAtUtc,
    this.onContinue,
    this.onBack,
  });

  final MobileMoneyPayoutFundingPlanningController controller;
  final String correlationId;
  final MobileMoneyPayoutFundingIntent intent;
  final DateTime requestedAtUtc;
  final ValueChanged<MobileMoneyPayoutFundingPlan>? onContinue;
  final VoidCallback? onBack;

  @override
  State<MobileMoneyPayoutFundingPlanningPage> createState() =>
      _MobileMoneyPayoutFundingPlanningPageState();
}

class _MobileMoneyPayoutFundingPlanningPageState
    extends State<MobileMoneyPayoutFundingPlanningPage> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerChanged);
    WidgetsBinding.instance.addPostFrameCallback((_) => _planFunding());
  }

  @override
  void didUpdateWidget(
    covariant MobileMoneyPayoutFundingPlanningPage oldWidget,
  ) {
    super.didUpdateWidget(oldWidget);

    final controllerChanged = oldWidget.controller != widget.controller;
    final requestChanged =
        oldWidget.correlationId != widget.correlationId ||
            oldWidget.intent != widget.intent ||
            oldWidget.requestedAtUtc != widget.requestedAtUtc;

    if (controllerChanged) {
      oldWidget.controller.removeListener(_onControllerChanged);
      widget.controller.addListener(_onControllerChanged);
    }

    if (controllerChanged || requestChanged) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _planFunding());
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

  Future<void> _planFunding() {
    return widget.controller.planFunding(
      correlationId: widget.correlationId,
      intent: widget.intent,
      requestedAtUtc: widget.requestedAtUtc,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Plan de paiement'),
        leading: widget.onBack == null
            ? null
            : IconButton(
                key: const Key('momo-funding-plan-back'),
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
      case MobileMoneyPayoutFundingPlanningPresentationStatus.idle:
      case MobileMoneyPayoutFundingPlanningPresentationStatus.loading:
        return const _LoadingView();
      case MobileMoneyPayoutFundingPlanningPresentationStatus.ready:
        final plan = widget.controller.plan;
        if (plan == null) {
          return _FailedView(onRetry: _planFunding);
        }
        return _ReadyView(
          plan: plan,
          onContinue: widget.onContinue,
        );
      case MobileMoneyPayoutFundingPlanningPresentationStatus.failed:
        return _FailedView(onRetry: _planFunding);
    }
  }
}

class _LoadingView extends StatelessWidget {
  const _LoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(
      key: Key('momo-funding-plan-loading'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircularProgressIndicator(),
          SizedBox(height: 16),
          Text(
            'Validation de votre plan de paiement…',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

class _ReadyView extends StatelessWidget {
  const _ReadyView({
    required this.plan,
    required this.onContinue,
  });

  final MobileMoneyPayoutFundingPlan plan;
  final ValueChanged<MobileMoneyPayoutFundingPlan>? onContinue;

  @override
  Widget build(BuildContext context) {
    return ListView(
      key: const Key('momo-funding-plan-ready'),
      children: [
        Text(
          'Plan de paiement confirmé',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 8),
        const Text(
          'AfrikaWallet a validé la répartition du financement avant l’exécution du transfert.',
        ),
        const SizedBox(height: 20),
        Card(
          key: const Key('momo-funding-plan-total'),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: _PlanRow(
              label: 'Total à financer',
              value: _formatMinor(
                plan.requiredAmountMinor,
                plan.currencyCode,
              ),
            ),
          ),
        ),
        const SizedBox(height: 12),
        Card(
          key: const Key('momo-funding-plan-allocations'),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Répartition confirmée',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 8),
                for (final allocation in plan.allocations)
                  _PlanRow(
                    key: Key(
                      'momo-funding-plan-allocation-${allocation.sourceId}',
                    ),
                    label: _sourceLabel(allocation.sourceType),
                    value: _formatMinor(
                      allocation.amountMinor,
                      allocation.currencyCode,
                    ),
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Text(
          'Plan préparé le '
          '${DateFormat('dd/MM/yyyy HH:mm').format(plan.plannedAtUtc.toLocal())}.',
          key: const Key('momo-funding-plan-planned-at'),
        ),
        const SizedBox(height: 12),
        const Text(
          'Aucun débit ni transfert Mobile Money n’a encore été lancé.',
          key: Key('momo-funding-plan-no-payout'),
        ),
        if (onContinue != null) ...[
          const SizedBox(height: 24),
          FilledButton(
            key: const Key('momo-funding-plan-continue'),
            onPressed: () => onContinue!(plan),
            child: const Text('Continuer'),
          ),
        ],
      ],
    );
  }
}

class _PlanRow extends StatelessWidget {
  const _PlanRow({
    super.key,
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Text(label)),
          const SizedBox(width: 16),
          Flexible(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
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
      key: const Key('momo-funding-plan-failed'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off_outlined, size: 56),
          const SizedBox(height: 16),
          Text(
            'Plan de paiement indisponible',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'La répartition du financement n’a pas pu être validée. Aucun débit ni transfert n’a été lancé.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          OutlinedButton(
            key: const Key('momo-funding-plan-retry'),
            onPressed: onRetry,
            child: const Text('Réessayer'),
          ),
        ],
      ),
    );
  }
}

String _sourceLabel(FundingSourceType type) {
  return switch (type) {
    FundingSourceType.wallet => 'Solde AfrikaWallet',
    FundingSourceType.applePay => 'Apple Pay',
    FundingSourceType.googlePay => 'Google Pay',
    FundingSourceType.sepa => 'SEPA',
    FundingSourceType.paymentCard => 'Carte bancaire',
  };
}

String _formatMinor(int amountMinor, String currencyCode) {
  final formatter = NumberFormat.currency(
    name: currencyCode,
    symbol: currencyCode,
  );
  final decimalDigits = formatter.decimalDigits ?? 2;
  final divisor = math.pow(10, decimalDigits);
  return formatter.format(amountMinor / divisor);
}
