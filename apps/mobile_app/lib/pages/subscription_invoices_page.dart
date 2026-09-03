import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';
import '../services/subscription_repository.dart';
import 'subscription_invoice_detail_page.dart';

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
  static const String _allStatusesValue = '__all__';

  bool _isLoading = true;
  bool _isError = false;
  String _selectedStatus = _allStatusesValue;
  List<SubscriptionInvoice> _invoices = const [];

  List<String> get _availableStatuses {
    final statuses = <String>{};
    for (final invoice in _invoices) {
      if (invoice.status.isNotEmpty) {
        statuses.add(invoice.status);
      }
    }
    return statuses.toList();
  }

  List<SubscriptionInvoice> get _visibleInvoices {
    if (_selectedStatus == _allStatusesValue) {
      return _invoices;
    }
    return _invoices.where((invoice) => invoice.status == _selectedStatus).toList();
  }

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
      setState(() {
        _invoices = invoices;
        if (_selectedStatus != _allStatusesValue && !invoices.any((invoice) => invoice.status == _selectedStatus)) {
          _selectedStatus = _allStatusesValue;
        }
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _isError = true);
    }

    if (!mounted) return;
    setState(() => _isLoading = false);
  }

  void _openInvoiceDetail(SubscriptionInvoice invoice) {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (detailContext) => SubscriptionInvoiceDetailPage(
          invoice: invoice,
          onReturnToBillingHistory: () => Navigator.of(detailContext).pop(),
        ),
      ),
    );
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

    final visibleInvoices = _visibleInvoices;
    final statuses = _availableStatuses;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: DropdownButtonFormField<String>(
            key: const Key('subscription-invoice-status-filter'),
            initialValue: _selectedStatus,
            decoration: InputDecoration(
              labelText: localizations.invoiceStatusFilter,
              border: const OutlineInputBorder(),
            ),
            items: [
              DropdownMenuItem<String>(
                value: _allStatusesValue,
                child: Text(localizations.allInvoiceStatuses),
              ),
              ...statuses.map(
                (status) => DropdownMenuItem<String>(
                  value: status,
                  child: Text(status),
                ),
              ),
            ],
            onChanged: (value) {
              if (value == null) return;
              setState(() => _selectedStatus = value);
            },
          ),
        ),
        Expanded(
          child: visibleInvoices.isEmpty
              ? Center(
                  key: const Key('subscription-invoices-filtered-empty'),
                  child: Text(localizations.noInvoicesForStatus),
                )
              : ListView.builder(
                  key: const Key('subscription-invoices-page'),
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                  itemCount: visibleInvoices.length,
                  itemBuilder: (context, index) {
                    final invoice = visibleInvoices[index];
                    return Card(
                      key: Key('subscription-invoice-${invoice.id}'),
                      margin: const EdgeInsets.only(bottom: 12),
                      child: InkWell(
                        key: Key('subscription-invoice-open-${invoice.id}'),
                        onTap: () => _openInvoiceDetail(invoice),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Text(
                                      '${localizations.invoiceId}: ${invoice.id}',
                                      style: const TextStyle(fontWeight: FontWeight.bold),
                                    ),
                                  ),
                                  const Icon(Icons.chevron_right),
                                ],
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
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }
}
