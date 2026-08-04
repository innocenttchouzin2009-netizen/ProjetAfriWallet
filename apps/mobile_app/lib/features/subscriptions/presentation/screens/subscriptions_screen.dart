import 'package:flutter/material.dart';

import '../../application/subscription_catalog_controller.dart';
import '../../data/subscription_catalog_repository.dart';
import '../widgets/subscription_provider_card.dart';
import 'subscription_detail_screen.dart';

class SubscriptionsScreen extends StatefulWidget {
  const SubscriptionsScreen({super.key});

  @override
  State<SubscriptionsScreen> createState() => _SubscriptionsScreenState();
}

class _SubscriptionsScreenState extends State<SubscriptionsScreen> {
  late final SubscriptionCatalogController _controller;

  @override
  void initState() {
    super.initState();
    _controller = SubscriptionCatalogController(
      repository: DemoSubscriptionCatalogRepository(),
    )..load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Abonnements'),
      ),
      body: AnimatedBuilder(
        animation: _controller,
        builder: (BuildContext context, Widget? child) {
          return switch (_controller.status) {
            SubscriptionCatalogStatus.initial || SubscriptionCatalogStatus.loading => const Center(
                child: CircularProgressIndicator(),
              ),
            SubscriptionCatalogStatus.error => _ErrorView(
                message: _controller.errorMessage ?? 'Une erreur est survenue.',
                onRetry: _controller.load,
              ),
            SubscriptionCatalogStatus.success => RefreshIndicator(
                onRefresh: _controller.load,
                child: ListView(
                  padding: const EdgeInsets.all(16),
                  children: <Widget>[
                    Text(
                      'Tous vos services au même endroit',
                      style: Theme.of(context).textTheme.headlineSmall,
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Choisissez un fournisseur pour consulter ses offres ou préparer un renouvellement.',
                    ),
                    const SizedBox(height: 20),
                    for (final provider in _controller.providers) ...<Widget>[
                      SubscriptionProviderCard(
                        provider: provider,
                        onTap: () {
                          Navigator.of(context).push(
                            MaterialPageRoute<void>(
                              builder: (_) => SubscriptionDetailScreen(provider: provider),
                            ),
                          );
                        },
                      ),
                      const SizedBox(height: 10),
                    ],
                    const SizedBox(height: 8),
                    OutlinedButton.icon(
                      onPressed: () {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text('La demande d’ajout sera connectée au backend.'),
                          ),
                        );
                      },
                      icon: const Icon(Icons.add),
                      label: const Text('Demander un autre abonnement'),
                    ),
                  ],
                ),
              ),
          };
        },
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({
    required this.message,
    required this.onRetry,
  });

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            const Icon(Icons.error_outline, size: 44),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: onRetry,
              child: const Text('Réessayer'),
            ),
          ],
        ),
      ),
    );
  }
}
