import 'package:flutter/material.dart';

import '../../domain/subscription_provider.dart';

class SubscriptionDetailScreen extends StatelessWidget {
  const SubscriptionDetailScreen({
    required this.provider,
    super.key,
  });

  final SubscriptionProvider provider;

  @override
  Widget build(BuildContext context) {
    final isAvailable = provider.isAvailable;

    return Scaffold(
      appBar: AppBar(
        title: Text(provider.name),
      ),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: <Widget>[
          Center(
            child: CircleAvatar(
              radius: 42,
              child: Text(
                provider.iconText,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
            ),
          ),
          const SizedBox(height: 20),
          Text(
            provider.name,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 10),
          Text(
            provider.description,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 28),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: isAvailable
                  ? const Text('Sélectionnez une formule.')
                  : const Column(
                      children: <Widget>[
                        Icon(Icons.schedule, size: 32),
                        SizedBox(height: 10),
                        Text(
                          'Ce service sera bientôt disponible dans AfriWallet.',
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
            ),
          ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: isAvailable
                ? () {
                    // À connecter plus tard au Payment Intent Engine.
                  }
                : null,
            child: Text(isAvailable ? 'Choisir une formule' : 'Bientôt disponible'),
          ),
        ],
      ),
    );
  }
}
