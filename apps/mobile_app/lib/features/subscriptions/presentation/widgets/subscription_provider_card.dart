import 'package:flutter/material.dart';

import '../../domain/subscription_provider.dart';

class SubscriptionProviderCard extends StatelessWidget {
  const SubscriptionProviderCard({
    required this.provider,
    required this.onTap,
    super.key,
  });

  final SubscriptionProvider provider;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    final statusText = switch (provider.status) {
      SubscriptionProviderStatus.available => 'Disponible',
      SubscriptionProviderStatus.comingSoon => 'Bientôt disponible',
      SubscriptionProviderStatus.unavailable => 'Indisponible',
    };

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: <Widget>[
              CircleAvatar(
                radius: 26,
                child: Text(
                  provider.iconText,
                  textAlign: TextAlign.center,
                  style: theme.textTheme.titleMedium,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      provider.name,
                      style: theme.textTheme.titleMedium,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      provider.description,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.bodyMedium,
                    ),
                    const SizedBox(height: 8),
                    Text(
                      statusText,
                      style: theme.textTheme.labelMedium,
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
}
