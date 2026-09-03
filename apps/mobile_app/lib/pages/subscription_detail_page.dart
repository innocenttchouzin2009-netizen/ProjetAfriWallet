import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';

class SubscriptionDetailPage extends StatelessWidget {
  const SubscriptionDetailPage({
    super.key,
    required this.subscription,
    this.onReturnToSubscriptions,
    this.onReturnToWallet,
    this.onOpenBillingHistory,
  });

  final UserSubscription subscription;
  final VoidCallback? onReturnToSubscriptions;
  final VoidCallback? onReturnToWallet;
  final VoidCallback? onOpenBillingHistory;

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        leading: BackButton(
          key: const Key('subscription-detail-return-subscriptions'),
          onPressed: onReturnToSubscriptions ?? () => Navigator.of(context).pop(),
        ),
        title: Text(
          subscription.name,
          key: const Key('subscription-detail-title'),
        ),
      ),
      body: SafeArea(
        child: ListView(
          key: const Key('subscription-detail-page'),
          padding: const EdgeInsets.all(20),
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    subscription.name,
                    key: const Key('subscription-detail-name'),
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                ),
                Chip(
                  key: const Key('subscription-detail-status'),
                  label: Text(subscription.status),
                ),
              ],
            ),
            const SizedBox(height: 20),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _DetailLine(label: 'Provider', value: subscription.providerId),
                    _DetailLine(label: localizations.cycle, value: subscription.currentCycle),
                    _DetailLine(label: localizations.nextBilling, value: subscription.nextBillingDate),
                    _DetailLine(
                      label: localizations.price,
                      value: '${localizations.formatCurrency(subscription.price)} ${subscription.currency}',
                    ),
                    _DetailLine(
                      label: localizations.autoRenewal,
                      value: subscription.autoRenew ? 'On' : 'Off',
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            SwitchListTile(
              key: const Key('subscription-detail-auto-renew-readonly'),
              value: subscription.autoRenew,
              onChanged: null,
              title: Text(localizations.autoRenewal),
            ),
            if (onOpenBillingHistory != null) ...[
              const SizedBox(height: 16),
              FilledButton.icon(
                key: const Key('subscription-detail-billing-history'),
                onPressed: onOpenBillingHistory,
                icon: const Icon(Icons.receipt_long_outlined),
                label: Text(localizations.billingHistory),
              ),
            ],
            if (onReturnToWallet != null) ...[
              const SizedBox(height: 16),
              OutlinedButton.icon(
                key: const Key('subscription-detail-return-wallet'),
                onPressed: onReturnToWallet,
                icon: const Icon(Icons.account_balance_wallet_outlined),
                label: Text(localizations.wallet),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _DetailLine extends StatelessWidget {
  const _DetailLine({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Text(label, style: const TextStyle(fontWeight: FontWeight.w600))),
          const SizedBox(width: 16),
          Expanded(child: Text(value, textAlign: TextAlign.end)),
        ],
      ),
    );
  }
}
