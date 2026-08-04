import 'package:flutter/material.dart';

import '../models/subscription_models.dart';
import '../services/subscription_repository.dart';

class SubscriptionsPage extends StatefulWidget {
  const SubscriptionsPage({super.key, this.repository});

  final SubscriptionRepository? repository;

  @override
  State<SubscriptionsPage> createState() => _SubscriptionsPageState();
}

class _SubscriptionsPageState extends State<SubscriptionsPage> {
  late final SubscriptionRepository _repository = widget.repository ?? ApiSubscriptionRepository();
  final TextEditingController _searchController = TextEditingController();
  final List<String> _countries = ['CM', 'CI', 'SN'];
  final List<String> _currencies = ['XOF', 'EUR'];

  String? _selectedCountry;
  String? _selectedCurrency;
  bool _isLoading = true;
  bool _isError = false;
  List<SubscriptionOffer> _offers = [];
  List<UserSubscription> _subscriptions = [];

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _isError = false;
    });

    try {
      final offersFuture = _repository.fetchOffers(
        country: _selectedCountry,
        currency: _selectedCurrency,
        query: _searchController.text,
      );
      final subscriptionsFuture = _repository.fetchUserSubscriptions();
      final results = await Future.wait([offersFuture, subscriptionsFuture]);
      setState(() {
        _offers = results[0] as List<SubscriptionOffer>;
        _subscriptions = results[1] as List<UserSubscription>;
      });
    } catch (_) {
      setState(() {
        _isError = true;
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Abonnements'),
      ),
      body: RefreshIndicator(
        onRefresh: _loadData,
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_isError) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text('Unable to load subscriptions'),
            const SizedBox(height: 12),
            FilledButton(onPressed: _loadData, child: const Text('Retry')),
          ],
        ),
      );
    }

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _buildSearchSection(),
        const SizedBox(height: 16),
        _buildSubscriptionsSection(),
        const SizedBox(height: 16),
        _buildOffersSection(),
      ],
    );
  }

  Widget _buildSearchSection() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Catalogue', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            TextField(
              controller: _searchController,
              decoration: const InputDecoration(
                labelText: 'Rechercher une offre',
                border: OutlineInputBorder(),
              ),
              onSubmitted: (_) => _loadData(),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _selectedCountry,
                    decoration: const InputDecoration(labelText: 'Pays', border: OutlineInputBorder()),
                    items: _countries.map((country) => DropdownMenuItem(value: country, child: Text(country))).toList(),
                    onChanged: (value) {
                      setState(() => _selectedCountry = value);
                      _loadData();
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _selectedCurrency,
                    decoration: const InputDecoration(labelText: 'Devise', border: OutlineInputBorder()),
                    items: _currencies.map((currency) => DropdownMenuItem(value: currency, child: Text(currency))).toList(),
                    onChanged: (value) {
                      setState(() => _selectedCurrency = value);
                      _loadData();
                    },
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSubscriptionsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Mes abonnements', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        if (_subscriptions.isEmpty)
          const Text('Aucun abonnement pour le moment.')
        else
          ..._subscriptions.map((subscription) => _SubscriptionCard(subscription: subscription)),
      ],
    );
  }

  Widget _buildOffersSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Offres', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        if (_offers.isEmpty)
          const Text('Aucune offre disponible.')
        else
          ..._offers.map((offer) => _OfferCard(offer: offer, repository: _repository)),
      ],
    );
  }
}

class _SubscriptionCard extends StatelessWidget {
  const _SubscriptionCard({required this.subscription});

  final UserSubscription subscription;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(subscription.name, style: const TextStyle(fontWeight: FontWeight.bold))),
                Chip(label: Text(subscription.status)),
              ],
            ),
            const SizedBox(height: 8),
            Text('Cycle: ${subscription.currentCycle}'),
            Text('Prochaine facturation: ${subscription.nextBillingDate}'),
            Text('Prix: ${subscription.price} ${subscription.currency}'),
            const SizedBox(height: 8),
            Row(
              children: [
                Switch(value: subscription.autoRenew, onChanged: (_) {}),
                const Text('Auto-renouvellement'),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _OfferCard extends StatelessWidget {
  const _OfferCard({required this.offer, required this.repository});

  final SubscriptionOffer offer;
  final SubscriptionRepository repository;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(offer.name, style: const TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            Text(offer.description),
            const SizedBox(height: 8),
            Text('${offer.price} ${offer.currency} / mois'),
            const SizedBox(height: 8),
            Wrap(spacing: 8, children: offer.features.map((feature) => Chip(label: Text(feature))).toList()),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: FilledButton(
                    onPressed: () async {
                      await repository.createSubscription(offer.id);
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Abonnement créé')));
                      }
                    },
                    child: const Text('Souscrire'),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton(
                    onPressed: () {
                      showDialog<void>(
                        context: context,
                        builder: (dialogContext) => AlertDialog(
                          title: Text(offer.name),
                          content: Text(offer.longDescription),
                          actions: [
                            TextButton(onPressed: () => Navigator.of(dialogContext).pop(), child: const Text('Fermer')),
                          ],
                        ),
                      );
                    },
                    child: const Text('Détails'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
