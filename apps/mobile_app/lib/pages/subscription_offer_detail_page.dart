import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';

class SubscriptionOfferDetailPage extends StatelessWidget {
  const SubscriptionOfferDetailPage({
    super.key,
    required this.offer,
    this.onContinue,
  });

  final SubscriptionOffer offer;
  final VoidCallback? onContinue;

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          offer.name,
          key: const Key('subscription-offer-detail-title'),
        ),
      ),
      body: ListView(
        key: const Key('subscription-offer-detail-page'),
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            offer.name,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          Text(
            offer.longDescription.isEmpty ? offer.description : offer.longDescription,
            key: const Key('subscription-offer-detail-description'),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${localizations.price}: ${localizations.formatCurrency(offer.price)} ${offer.currency} / ${localizations.monthly}',
                    key: const Key('subscription-offer-detail-price'),
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      if (offer.providerId.isNotEmpty)
                        Chip(
                          key: const Key('subscription-offer-detail-provider'),
                          avatar: const Icon(Icons.business, size: 18),
                          label: Text(offer.providerId),
                        ),
                      if (offer.country.isNotEmpty)
                        Chip(
                          key: const Key('subscription-offer-detail-country'),
                          avatar: const Icon(Icons.public, size: 18),
                          label: Text(offer.country),
                        ),
                      if (offer.category.isNotEmpty)
                        Chip(
                          key: const Key('subscription-offer-detail-category'),
                          avatar: const Icon(Icons.category_outlined, size: 18),
                          label: Text(offer.category),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          if (offer.features.isNotEmpty) ...[
            const SizedBox(height: 16),
            Wrap(
              key: const Key('subscription-offer-detail-features'),
              spacing: 8,
              runSpacing: 8,
              children: offer.features
                  .map(
                    (feature) => Chip(
                      avatar: const Icon(Icons.check_circle_outline, size: 18),
                      label: Text(feature),
                    ),
                  )
                  .toList(),
            ),
          ],
          if (onContinue != null) ...[
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('subscription-offer-detail-continue'),
              onPressed: onContinue,
              child: Text(localizations.subscribe),
            ),
          ],
        ],
      ),
    );
  }
}
