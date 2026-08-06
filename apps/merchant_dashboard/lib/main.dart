import 'package:flutter/material.dart';
import 'package:afw_design_system/afw_design_system.dart';

void main() {
  runApp(const MerchantDashboardApp());
}

class MerchantDashboardApp extends StatelessWidget {
  const MerchantDashboardApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AfriWallet Merchant Dashboard',
      theme: AfwTheme.light(),
      home: const MerchantDashboardHome(),
    );
  }
}

class MerchantDashboardHome extends StatelessWidget {
  const MerchantDashboardHome({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Merchant Dashboard'),
        actions: [
          IconButton(onPressed: () {}, icon: const Icon(Icons.notifications_outlined)),
        ],
      ),
      body: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Overview', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 12),
              Wrap(
                spacing: 12,
                runSpacing: 12,
                children: const [
                  _SummaryCard(title: 'Today sales', value: 'XAF 150,000'),
                  _SummaryCard(title: 'Pending payments', value: '18'),
                  _SummaryCard(title: 'Settlements done', value: '12'),
                  _SummaryCard(title: 'Pending settlements', value: '4'),
                ],
              ),
              const SizedBox(height: 20),
              Text('Revenue analytics', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Weekly growth trending above target.'),
                      const SizedBox(height: 8),
                      const Text('Peak hour: 12:00 - 14:00'),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 20),
              Text('Recent transactions', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              const _TransactionTile(label: 'QR payment', value: 'XAF 7,500'),
              const _TransactionTile(label: 'POS checkout', value: 'XAF 12,000'),
              const _TransactionTile(label: 'Settlement received', value: 'XAF 35,000'),
              const SizedBox(height: 20),
              Text('QR / POS overview', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              const _QuickActionTile(label: 'QR payments', description: '5 active codes'),
              const _QuickActionTile(label: 'POS terminals', description: '3 live terminals'),
            ],
          ),
        ),
      ),
    );
  }
}

class _SummaryCard extends StatelessWidget {
  final String title;
  final String value;

  const _SummaryCard({required this.title, required this.value});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 180,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              Text(value, style: Theme.of(context).textTheme.headlineSmall),
            ],
          ),
        ),
      ),
    );
  }
}

class _TransactionTile extends StatelessWidget {
  final String label;
  final String value;

  const _TransactionTile({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        title: Text(label),
        trailing: Text(value),
      ),
    );
  }
}

class _QuickActionTile extends StatelessWidget {
  final String label;
  final String description;

  const _QuickActionTile({required this.label, required this.description});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: const Icon(Icons.account_balance_wallet_outlined),
        title: Text(label),
        subtitle: Text(description),
      ),
    );
  }
}
