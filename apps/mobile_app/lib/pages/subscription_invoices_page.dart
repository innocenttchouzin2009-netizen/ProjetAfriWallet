import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';
import '../presentation/subscription_invoice_status_presentation.dart';
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
  static const String _sortNewestFirst = 'newest';
  static const String _sortOldestFirst = 'oldest';
  static const String _sortAmountLowToHigh = 'amount-asc';
  static const String _sortAmountHighToLow = 'amount-desc';

  bool _isLoading = true;
  bool _isError = false;
  String _selectedStatus = _allStatusesValue;
  String _selectedSort = _sortNewestFirst;
  String _searchQuery = '';
  List<SubscriptionInvoice> _invoices = const [];

  List<String> get _availableStatuses {
    final statuses = <String>{};
    for (final invoice in _invoices) {
      if (invoice.status.isNotEmpty) statuses.add(invoice.status);
    }
    return statuses.toList();
  }

  List<SubscriptionInvoice> get _visibleInvoices {
    final normalizedQuery = _searchQuery.trim().toLowerCase();
    final visibleInvoices = _invoices.where((invoice) {
      final matchesStatus = _selectedStatus == _allStatusesValue || invoice.status == _selectedStatus;
      final matchesSearch = normalizedQuery.isEmpty || invoice.id.toLowerCase().contains(normalizedQuery);
      return matchesStatus && matchesSearch;
    }).toList();
    visibleInvoices.sort(_compareInvoices);
    return visibleInvoices;
  }

  int _compareInvoices(SubscriptionInvoice first, SubscriptionInvoice second) {
    int comparison;
    switch (_selectedSort) {
      case _sortOldestFirst:
        comparison = _compareIssueDates(first, second);
        break;
      case _sortAmountLowToHigh:
        comparison = first.amount.compareTo(second.amount);
        break;
      case _sortAmountHighToLow:
        comparison = second.amount.compareTo(first.amount);
        break;
      case _sortNewestFirst:
      default:
        comparison = _compareIssueDates(second, first);
        break;
    }
    return comparison != 0 ? comparison : first.id.compareTo(second.id);
  }

  int _compareIssueDates(SubscriptionInvoice first, SubscriptionInvoice second) {
    final firstDate = DateTime.tryParse(first.issueDate);
    final secondDate = DateTime.tryParse(second.issueDate);
    if (firstDate != null && secondDate != null) {
      return firstDate.compareTo(secondDate);
    }
    return first.issueDate.compareTo(second.issueDate);
  }

  String _sortLabel(AppLocalizations localizations, String sort) {
    switch (sort) {
      case _sortOldestFirst:
        return localizations.invoiceSortOldestFirst;
      case _sortAmountLowToHigh:
        return localizations.invoiceSortAmountLowToHigh;
      case _sortAmountHighToLow:
        return localizations.invoiceSortAmountHighToLow;
      case _sortNewestFirst:
      default:
        return localizations.invoiceSortNewestFirst;
    }
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
      body: SafeArea(child: _buildBody(localizations)),
    );
  }

  Widget _buildBody(AppLocalizations localizations) {
    if (_isLoading) {
      return const Center(key: Key('subscription-invoices-loading'), child: CircularProgressIndicator());
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
      return Center(key: const Key('subscription-invoices-empty'), child: Text(localizations.noInvoices));
    }

    final visibleInvoices = _visibleInvoices;
    final statuses = _availableStatuses;
    final hasSearch = _searchQuery.trim().isNotEmpty;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: TextField(
            key: const Key('subscription-invoice-search'),
            decoration: InputDecoration(
              labelText: localizations.invoiceSearch,
              hintText: localizations.invoiceSearchHint,
              prefixIcon: const Icon(Icons.search),
              border: const OutlineInputBorder(),
            ),
            textInputAction: TextInputAction.search,
            onChanged: (value) => setState(() => _searchQuery = value),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
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
                  child: Text(localizeSubscriptionInvoiceStatus(localizations, status)),
                ),
              ),
            ],
            onChanged: (value) {
              if (value == null) return;
              setState(() => _selectedStatus = value);
            },
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: DropdownButtonFormField<String>(
            key: const Key('subscription-invoice-sort'),
            initialValue: _selectedSort,
            decoration: InputDecoration(
              labelText: localizations.invoiceSort,
              border: const OutlineInputBorder(),
            ),
            items: const [
              _sortNewestFirst,
              _sortOldestFirst,
              _sortAmountLowToHigh,
              _sortAmountHighToLow,
            ]
                .map(
                  (sort) => DropdownMenuItem<String>(
                    value: sort,
                    child: Text(_sortLabel(localizations, sort)),
                  ),
                )
                .toList(),
            onChanged: (value) {
              if (value == null) return;
              setState(() => _selectedSort = value);
            },
          ),
        ),
        Expanded(
          child: visibleInvoices.isEmpty
              ? Center(
                  key: Key(hasSearch ? 'subscription-invoices-search-empty' : 'subscription-invoices-filtered-empty'),
                  child: Text(hasSearch ? localizations.noInvoicesForSearch : localizations.noInvoicesForStatus),
                )
              : ListView.builder(
                  key: const Key('subscription-invoices-page'),
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                  itemCount: visibleInvoices.length,
                  itemBuilder: (context, index) {
                    final invoice = visibleInvoices[index];
                    final statusLabel = localizeSubscriptionInvoiceStatus(localizations, invoice.status);
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
                              Row(
                                children: [
                                  Text('${localizations.invoiceStatus}: '),
                                  Chip(
                                    key: Key('subscription-invoice-status-${invoice.id}'),
                                    label: Text(statusLabel),
                                    visualDensity: VisualDensity.compact,
                                  ),
                                ],
                              ),
                              Text('${localizations.invoiceIssueDate}: ${invoice.issueDate}'),
                              Text('${localizations.price}: ${localizations.formatCurrency(invoice.amount)} ${invoice.currency}'),
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
