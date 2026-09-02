import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';

class SubscriptionActivationResultPage extends StatelessWidget {
  const SubscriptionActivationResultPage({
    super.key,
    required this.offer,
    this.onReturnToSubscriptions,
    this.onReturnToWallet,
  });

  final SubscriptionOffer offer;
  final VoidCallback? onReturnToSubscriptions;
  final VoidCallback? onReturnToWallet;

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          offer.name,
          key: const Key('subscription-activation-result-title'),
        ),
      ),
      body: SafeArea(
        child: ListView(
          key: const Key('subscription-activation-result-page'),
          padding: const EdgeInsets.all(20),
          children: [
            const Center(
              child: Icon(
                Icons.check_circle_outline,
                key: Key('subscription-activation-result-icon'),
                size: 72,
              ),
            ),
            const SizedBox(height: 24),
            Text(
              offer.name,
              key: const Key('subscription-activation-result-offer-name'),
              style: Theme.of(context).textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 12),
            Text(
              offer.longDescription.isEmpty ? offer.description : offer.longDescription,
              key: const Key('subscription-activation-result-description'),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 20),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      localizations.details,
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      '${localizations.price}: ${localizations.formatCurrency(offer.price)} ${offer.currency} / ${localizations.monthly}',
                      key: const Key('subscription-activation-result-price'),
                    ),
                    if (offer.providerId.isNotEmpty) ...[
                      const SizedBox(height: 8),
                      Text(
                        offer.providerId,
                        key: const Key('subscription-activation-result-provider'),
                      ),
                    ],
                  ],
                ),
              ),
            ),
            if (onReturnToSubscriptions != null) ...[
              const SizedBox(height: 24),
              FilledButton(
                key: const Key('subscription-activation-result-return-subscriptions'),
                onPressed: onReturnToSubscriptions,
                child: Text(localizations.mySubscriptions),
              ),
            ],
            if (onReturnToWallet != null) ...[
              const SizedBox(height: 12),
              OutlinedButton(
                key: const Key('subscription-activation-result-return-wallet'),
                onPressed: onReturnToWallet,
                child: Text(localizations.wallet),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
