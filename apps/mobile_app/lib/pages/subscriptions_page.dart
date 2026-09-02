import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';
import '../services/subscription_repository.dart';
import 'subscription_activation_result_page.dart';
import 'subscription_offer_detail_page.dart';

class SubscriptionsPage extends StatefulWidget {
  const SubscriptionsPage({
    super.key,
    this.repository,
    this.locale,
    this.onOpenSettings,
    this.onReturnToWallet,
  });

  final SubscriptionRepository? repository;
  final Locale? locale;
  final VoidCallback? onOpenSettings;
  final VoidCallback? onReturnToWallet;

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
    final localizations = AppLocalizations.of(context)!;
    return Scaffold(
      appBar: AppBar(
        leading: widget.onReturnToWallet == null
            ? null
            : BackButton(
                key: const Key('subscriptions-return-to-wallet'),
                onPressed: widget.onReturnToWallet,
              ),
        title: Text(localizations.appTitle),
        actions: [
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: widget.onOpenSettings,
          ),
        ],
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
      final localizations = AppLocalizations.of(context)!;
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(localizations.welcome),
            const SizedBox(height: 12),
            FilledButton(onPressed: _loadData, child: Text(localizations.retry)),
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
    final localizations = AppLocalizations.of(context)!;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(localizations.wallet, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            TextField(
              controller: _searchController,
              decoration: InputDecoration(
                labelText: localizations.searchOffers,
                border: const OutlineInputBorder(),
              ),
              onSubmitted: (_) => _loadData(),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _selectedCountry,
                    decoration: InputDecoration(labelText: localizations.country, border: const OutlineInputBorder()),
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
                    decoration: InputDecoration(labelText: localizations.currency, border: const OutlineInputBorder()),
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
    final localizations = AppLocalizations.of(context)!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(localizations.mySubscriptions, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        if (_subscriptions.isEmpty)
          Text(localizations.noSubscriptions)
        else
          ..._subscriptions.map((subscription) => _SubscriptionCard(subscription: subscription)),
      ],
    );
  }

  Widget _buildOffersSection() {
    final localizations = AppLocalizations.of(context)!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(localizations.offers, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        if (_offers.isEmpty)
          Text(localizations.noOffers)
        else
          ..._offers.map(
            (offer) => _OfferCard(
              offer: offer,
              onReturnToWallet: widget.onReturnToWallet,
            ),
          ),
      ],
    );
  }
}

class _SubscriptionCard extends StatelessWidget {
  const _SubscriptionCard({required this.subscription});

  final UserSubscription subscription;

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;
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
            Text('${localizations.cycle}: ${subscription.currentCycle}'),
            Text('${localizations.nextBilling}: ${subscription.nextBillingDate}'),
            Text('${localizations.price}: ${localizations.formatCurrency(subscription.price)} ${subscription.currency}'),
            const SizedBox(height: 8),
            Row(
              children: [
                Switch(value: subscription.autoRenew, onChanged: (_) {}),
                Text(localizations.autoRenewal),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _OfferCard extends StatelessWidget {
  const _OfferCard({required this.offer, this.onReturnToWallet});

  final SubscriptionOffer offer;
  final VoidCallback? onReturnToWallet;

  void _openActivationResult(BuildContext context) {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (resultContext) => SubscriptionActivationResultPage(
          offer: offer,
          onReturnToSubscriptions: () => Navigator.of(resultContext).pop(),
          onReturnToWallet: onReturnToWallet,
        ),
      ),
    );
  }

  void _openDetails(BuildContext context, {required bool allowConfirmation}) {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (detailContext) => SubscriptionOfferDetailPage(
          offer: offer,
          onContinue: allowConfirmation ? () => _openActivationResult(detailContext) : null,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;
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
            Text('${localizations.formatCurrency(offer.price)} ${offer.currency} / ${localizations.monthly}'),
            const SizedBox(height: 8),
            Wrap(spacing: 8, children: offer.features.map((feature) => Chip(label: Text(feature))).toList()),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: FilledButton(
                    onPressed: () => _openDetails(context, allowConfirmation: true),
                    child: Text(localizations.subscribe),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton(
                    onPressed: () => _openDetails(context, allowConfirmation: false),
                    child: Text(localizations.details),
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
