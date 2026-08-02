import 'package:afw_design_system/afw_design_system.dart';
import 'package:flutter/material.dart';

import '../../../../l10n/generated/app_localizations.dart';

class FinancialDeskScreen extends StatelessWidget {
  const FinancialDeskScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.financialDesk),
        actions: [
          IconButton(
            tooltip: 'Notifications',
            onPressed: () {},
            icon: const Icon(Icons.notifications_none_rounded),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(AfwSpacing.x2),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(AfwSpacing.x3),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(l10n.availableBalance),
                  const SizedBox(height: AfwSpacing.x1),
                  Text(
                    '€ 0,00',
                    style: Theme.of(context).textTheme.headlineLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: AfwSpacing.x1),
                  const Text('@afriwallet-user'),
                ],
              ),
            ),
          ),
          const SizedBox(height: AfwSpacing.x3),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _QuickAction(icon: Icons.send_rounded, label: l10n.send),
              _QuickAction(
                icon: Icons.call_received_rounded,
                label: l10n.receive,
              ),
              _QuickAction(
                icon: Icons.request_page_rounded,
                label: l10n.request,
              ),
              _QuickAction(
                icon: Icons.qr_code_scanner_rounded,
                label: l10n.scan,
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _QuickAction extends StatelessWidget {
  const _QuickAction({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: label,
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () {},
        child: Padding(
          padding: const EdgeInsets.all(AfwSpacing.x1),
          child: Column(
            children: [
              CircleAvatar(child: Icon(icon)),
              const SizedBox(height: AfwSpacing.x1),
              Text(label),
            ],
          ),
        ),
      ),
    );
  }
}
