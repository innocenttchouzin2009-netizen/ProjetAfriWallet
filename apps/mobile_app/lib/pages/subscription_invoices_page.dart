import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';
import '../services/subscription_repository.dart';

class SubscriptionInvoicesPage extends StatefulWidget {
  const SubscriptionInvoicesPage({
    super.key,
    required this.subscription,
    required this.repository,
    this.onReturnToSubscription,
  });

  final UserSubscription subscription;
  final SubscriptionRepository repository;
  final VoidCallback? onReturnToSubscription;

  @override
  State<SubscriptionInvoicesPage> createState() => _SubscriptionInvoicesPageState();
}

class _SubscriptionInvoicesPageState extends State<SubscriptionInvoicesPage> {
  bool _isLoading = true;
  bool _isError = false;
  List<SubscriptionInvoice> _invoices = const [];

  @override
  void initState() {
    super.initState();
    _loadInvoices();
  }

  Future<void> _loadInvoices() async {
    setState(() {
      _isLoading = true;
      _isError = false;
    });

    try {
      final invoices = await widget.repository.fetchInvoices(widget.subscription.id);
      if (!mounted) return;
      setState(() => _invoices = invoices);
    } catch (_) {
      if (!mounted) return;
      setState(() => _isError = true);
    }

    if (!mounted) return;
    setState(() => _isLoading = false);
  }

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        leading: BackButton(
          key: const Key('subscription-invoices-return-detail'),
          onPressed: widget.onReturnToSubscription ?? () => Navigator.of(context).pop(),
        ),
        title: Text(localizations.billingHistory),
      ),
      body: SafeArea(
        child: _buildBody(localizations),
      ),
    );
  }

  Widget _buildBody(AppLocalizations localizations) {
    if (_isLoading) {
      return const Center(
        key: Key('subscription-invoices-loading'),
        child: CircularProgressIndicator(),
      );
    }

    if (_isError) {
      return Center(
        key: const Key('subscription-invoices-error'),
        child: FilledButton(
          key: const Key('subscription-invoices-retry'),
          onPressed: _loadInvoices,
          child: Text(localizations.retry),
        ),
      );
    }

    if (_invoices.isEmpty) {
      return Center(
        key: const Key('subscription-invoices-empty'),
        child: Text(localizations.noInvoices),
      );
    }

    return ListView.builder(
      key: const Key('subscription-invoices-page'),
      padding: const EdgeInsets.all(16),
      itemCount: _invoices.length,
      itemBuilder: (context, index) {
        final invoice = _invoices[index];
        return Card(
          key: Key('subscription-invoice-${invoice.id}'),
          margin: const EdgeInsets.only(bottom: 12),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${localizations.invoiceId}: ${invoice.id}',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                Text('${localizations.invoiceStatus}: ${invoice.status}'),
                Text('${localizations.invoiceIssueDate}: ${invoice.issueDate}'),
                Text(
                  '${localizations.price}: ${localizations.formatCurrency(invoice.amount)} ${invoice.currency}',
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
