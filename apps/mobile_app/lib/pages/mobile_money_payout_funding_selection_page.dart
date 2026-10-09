import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../models/mobile_money_payout.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../presentation/mobile_money_payout_funding_selection_controller.dart';

class MobileMoneyPayoutFundingSelectionPage extends StatefulWidget {
  const MobileMoneyPayoutFundingSelectionPage({
    super.key,
    required this.controller,
    this.onContinue,
    this.onBack,
  });

  final MobileMoneyPayoutFundingSelectionController controller;
  final ValueChanged<MobileMoneyPayoutFundingIntent>? onContinue;
  final VoidCallback? onBack;

  @override
  State<MobileMoneyPayoutFundingSelectionPage> createState() =>
      _MobileMoneyPayoutFundingSelectionPageState();
}

class _MobileMoneyPayoutFundingSelectionPageState
    extends State<MobileMoneyPayoutFundingSelectionPage> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerChanged);
  }

  @override
  void didUpdateWidget(
    covariant MobileMoneyPayoutFundingSelectionPage oldWidget,
  ) {
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

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller;
    final wallet = controller.walletSource;
    final externalSources = controller.availableExternalSources;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Choisir le mode de paiement'),
        leading: widget.onBack == null
            ? null
            : IconButton(
                key: const Key('momo-funding-back'),
                onPressed: widget.onBack,
                icon: const Icon(Icons.arrow_back),
              ),
      ),
      body: SafeArea(
        child: ListView(
          key: const Key('momo-funding-selection-page'),
          padding: const EdgeInsets.all(20),
          children: [
            Text(
              'Comment souhaitez-vous payer ?',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            const Text(
              'Choisissez le solde AfrikaWallet, un moyen externe ou une combinaison lorsque votre solde est insuffisant.',
            ),
            const SizedBox(height: 20),
            _FundingSummary(
              totalMinor: controller.quote.totalSourceDebitMinor,
              currencyCode: controller.quote.sourceCurrencyCode,
            ),
            const SizedBox(height: 20),
            if (wallet != null)
              _WalletOption(
                wallet: wallet,
                totalMinor: controller.quote.totalSourceDebitMinor,
                selected: controller.selectedMode ==
                    MobileMoneyPayoutFundingSelectionMode.walletOnly,
                enabled: controller.canUseWalletOnly,
                onTap: controller.canUseWalletOnly
                    ? controller.selectWalletOnly
                    : null,
              ),
            if (externalSources.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text(
                'Autres moyens de paiement',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 8),
              for (final source in externalSources)
                _ExternalOption(
                  source: source,
                  selected: controller.selectedMode ==
                          MobileMoneyPayoutFundingSelectionMode.externalOnly &&
                      controller.selectedExternalSourceId == source.id,
                  onTap: () => controller.selectExternal(source),
                ),
            ],
            if (controller.canUseSplit) ...[
              const SizedBox(height: 20),
              Text(
                'Paiement combiné',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 8),
              for (final source in externalSources)
                _SplitOption(
                  wallet: wallet!,
                  externalSource: source,
                  walletAmountMinor: controller.splitWalletAmountMinor!,
                  externalAmountMinor: controller.splitExternalAmountMinor!,
                  currencyCode: controller.quote.sourceCurrencyCode,
                  selected: controller.selectedMode ==
                          MobileMoneyPayoutFundingSelectionMode.split &&
                      controller.selectedExternalSourceId == source.id,
                  onTap: () => controller.selectSplit(source),
                ),
            ],
            if (controller.selectedIntent != null) ...[
              const SizedBox(height: 20),
              _SelectedFundingSummary(
                intent: controller.selectedIntent!,
                sources: controller.fundingSources,
              ),
            ],
            const SizedBox(height: 16),
            const Text(
              'Aucun paiement ni transfert n’a encore été lancé.',
              key: Key('momo-funding-no-payout'),
            ),
            if (widget.onContinue != null) ...[
              const SizedBox(height: 24),
              FilledButton(
                key: const Key('momo-funding-continue'),
                onPressed: controller.selectedIntent == null
                    ? null
                    : () => widget.onContinue!(controller.selectedIntent!),
                child: const Text('Continuer'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _FundingSummary extends StatelessWidget {
  const _FundingSummary({
    required this.totalMinor,
    required this.currencyCode,
  });

  final int totalMinor;
  final String currencyCode;

  @override
  Widget build(BuildContext context) {
    return Card(
      key: const Key('momo-funding-total'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            const Expanded(child: Text('Total à payer')),
            Text(
              _formatMinor(totalMinor, currencyCode),
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
          ],
        ),
      ),
    );
  }
}

class _WalletOption extends StatelessWidget {
  const _WalletOption({
    required this.wallet,
    required this.totalMinor,
    required this.selected,
    required this.enabled,
    required this.onTap,
  });

  final FundingSource wallet;
  final int totalMinor;
  final bool selected;
  final bool enabled;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final balance = wallet.availableMinor!;
    final shortage = totalMinor - balance;

    return Card(
      child: ListTile(
        key: const Key('momo-funding-wallet'),
        enabled: enabled,
        selected: selected,
        onTap: onTap,
        leading: const Icon(Icons.account_balance_wallet_outlined),
        title: Text(wallet.displayLabel),
        subtitle: Text(
          enabled
              ? 'Solde disponible : ${_formatMinor(balance, wallet.currencyCode)}'
              : 'Solde insuffisant : il manque ${_formatMinor(shortage, wallet.currencyCode)}',
        ),
        trailing: Icon(
          selected ? Icons.check_circle : Icons.chevron_right,
        ),
      ),
    );
  }
}

class _ExternalOption extends StatelessWidget {
  const _ExternalOption({
    required this.source,
    required this.selected,
    required this.onTap,
  });

  final FundingSource source;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        key: Key('momo-funding-external-${source.id}'),
        selected: selected,
        onTap: onTap,
        leading: const Icon(Icons.credit_card_outlined),
        title: Text(source.displayLabel),
        subtitle: const Text('Payer la totalité avec ce moyen'),
        trailing: Icon(
          selected ? Icons.check_circle : Icons.chevron_right,
        ),
      ),
    );
  }
}

class _SplitOption extends StatelessWidget {
  const _SplitOption({
    required this.wallet,
    required this.externalSource,
    required this.walletAmountMinor,
    required this.externalAmountMinor,
    required this.currencyCode,
    required this.selected,
    required this.onTap,
  });

  final FundingSource wallet;
  final FundingSource externalSource;
  final int walletAmountMinor;
  final int externalAmountMinor;
  final String currencyCode;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        key: Key('momo-funding-split-${externalSource.id}'),
        selected: selected,
        onTap: onTap,
        leading: const Icon(Icons.call_split_outlined),
        title: Text('${wallet.displayLabel} + ${externalSource.displayLabel}'),
        subtitle: Text(
          '${_formatMinor(walletAmountMinor, currencyCode)} depuis le solde + '
          '${_formatMinor(externalAmountMinor, currencyCode)} via '
          '${externalSource.displayLabel}',
        ),
        trailing: Icon(
          selected ? Icons.check_circle : Icons.chevron_right,
        ),
      ),
    );
  }
}

class _SelectedFundingSummary extends StatelessWidget {
  const _SelectedFundingSummary({
    required this.intent,
    required this.sources,
  });

  final MobileMoneyPayoutFundingIntent intent;
  final List<FundingSource> sources;

  @override
  Widget build(BuildContext context) {
    final sourcesById = {
      for (final source in sources) source.id: source,
    };

    return Card(
      key: const Key('momo-funding-selected'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Paiement sélectionné',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 8),
            for (final allocation in intent.fundingAllocations)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(
                        sourcesById[allocation.sourceId]?.displayLabel ??
                            allocation.sourceId,
                      ),
                    ),
                    Text(
                      _formatMinor(
                        allocation.amountMinor,
                        allocation.currencyCode,
                      ),
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
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
