import 'package:flutter/material.dart';

import '../presentation/mobile_money_beneficiary_search_controller.dart';
import '../services/mobile_money_beneficiary_lookup_repository.dart';

class MobileMoneyBeneficiarySearchPage extends StatefulWidget {
  const MobileMoneyBeneficiarySearchPage({
    super.key,
    required this.controller,
    this.initialPhoneNumber = '',
    this.onContinue,
    this.onManualEntry,
    this.onBack,
  });

  final MobileMoneyBeneficiarySearchController controller;
  final String initialPhoneNumber;
  final ValueChanged<MobileMoneyBeneficiarySearchResult>? onContinue;
  final ValueChanged<String>? onManualEntry;
  final VoidCallback? onBack;

  @override
  State<MobileMoneyBeneficiarySearchPage> createState() =>
      _MobileMoneyBeneficiarySearchPageState();
}

class _MobileMoneyBeneficiarySearchPageState
    extends State<MobileMoneyBeneficiarySearchPage> {
  late final TextEditingController _phoneController;

  @override
  void initState() {
    super.initState();
    _phoneController = TextEditingController(
      text: widget.initialPhoneNumber,
    );
    widget.controller.addListener(_onControllerChanged);
  }

  @override
  void didUpdateWidget(covariant MobileMoneyBeneficiarySearchPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.controller != widget.controller) {
      oldWidget.controller.removeListener(_onControllerChanged);
      widget.controller.addListener(_onControllerChanged);
    }
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerChanged);
    _phoneController.dispose();
    super.dispose();
  }

  void _onControllerChanged() {
    if (mounted) {
      setState(() {});
    }
  }

  Future<void> _search() async {
    final phoneNumber = _phoneController.text.trim();
    if (phoneNumber.isEmpty || widget.controller.isSearching) {
      return;
    }

    FocusScope.of(context).unfocus();
    await widget.controller.search(phoneNumber: phoneNumber);
  }

  void _manualEntry() {
    widget.onManualEntry?.call(_phoneController.text.trim());
  }

  @override
  Widget build(BuildContext context) {
    final canSearch = _phoneController.text.trim().isNotEmpty &&
        !widget.controller.isSearching;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Ajouter un bénéficiaire'),
        leading: widget.onBack == null
            ? null
            : IconButton(
                key: const Key('momo-beneficiary-search-back'),
                onPressed: widget.onBack,
                icon: const Icon(Icons.arrow_back),
              ),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Text(
              'Rechercher un bénéficiaire Mobile Money',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            const Text(
              'Saisissez ou collez son numéro avec l’indicatif du pays. '
              'AfrikaWallet vérifiera les informations disponibles avant '
              'de continuer.',
            ),
            const SizedBox(height: 24),
            TextField(
              key: const Key('momo-beneficiary-search-input'),
              controller: _phoneController,
              keyboardType: TextInputType.phone,
              textInputAction: TextInputAction.search,
              autocorrect: false,
              decoration: const InputDecoration(
                labelText: 'Numéro Mobile Money',
                hintText: '+237 6 70 12 34 56',
                border: OutlineInputBorder(),
              ),
              onChanged: (_) => setState(() {}),
              onSubmitted: (_) {
                if (canSearch) {
                  _search();
                }
              },
            ),
            const SizedBox(height: 16),
            FilledButton.icon(
              key: const Key('momo-beneficiary-search-submit'),
              onPressed: canSearch ? _search : null,
              icon: const Icon(Icons.search),
              label: const Text('Rechercher'),
            ),
            if (widget.onManualEntry != null) ...[
              const SizedBox(height: 8),
              TextButton(
                key: const Key('momo-beneficiary-search-manual'),
                onPressed: widget.controller.isSearching ? null : _manualEntry,
                child: const Text('Saisir manuellement'),
              ),
            ],
            const SizedBox(height: 24),
            _SearchStatusView(
              controller: widget.controller,
              onContinue: widget.onContinue,
              onManualEntry:
                  widget.onManualEntry == null ? null : _manualEntry,
            ),
          ],
        ),
      ),
    );
  }
}

class _SearchStatusView extends StatelessWidget {
  const _SearchStatusView({
    required this.controller,
    required this.onContinue,
    required this.onManualEntry,
  });

  final MobileMoneyBeneficiarySearchController controller;
  final ValueChanged<MobileMoneyBeneficiarySearchResult>? onContinue;
  final VoidCallback? onManualEntry;

  @override
  Widget build(BuildContext context) {
    switch (controller.status) {
      case MobileMoneyBeneficiarySearchStatus.idle:
        return const SizedBox.shrink();
      case MobileMoneyBeneficiarySearchStatus.searching:
        return const _SearchingView();
      case MobileMoneyBeneficiarySearchStatus.resolved:
        final result = controller.result;
        if (result == null) {
          return const _FailedView();
        }
        return _ResolvedBeneficiaryView(
          result: result,
          onContinue: onContinue,
        );
      case MobileMoneyBeneficiarySearchStatus.manualEntryRequired:
        return _ManualEntryRequiredView(
          result: controller.result,
          onManualEntry: onManualEntry,
        );
      case MobileMoneyBeneficiarySearchStatus.failed:
        return const _FailedView();
    }
  }
}

class _SearchingView extends StatelessWidget {
  const _SearchingView();

  @override
  Widget build(BuildContext context) {
    return const Column(
      key: Key('momo-beneficiary-search-loading'),
      children: [
        LinearProgressIndicator(),
        SizedBox(height: 12),
        Text(
          'Recherche du bénéficiaire…',
          textAlign: TextAlign.center,
        ),
      ],
    );
  }
}

class _ResolvedBeneficiaryView extends StatelessWidget {
  const _ResolvedBeneficiaryView({
    required this.result,
    required this.onContinue,
  });

  final MobileMoneyBeneficiarySearchResult result;
  final ValueChanged<MobileMoneyBeneficiarySearchResult>? onContinue;

  @override
  Widget build(BuildContext context) {
    final lookup = result.lookup;
    final operator = result.operator;

    return Card(
      key: const Key('momo-beneficiary-search-resolved'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const Icon(Icons.verified_outlined),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Bénéficiaire trouvé',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            _BeneficiaryDetail(
              key: const Key('momo-beneficiary-search-name'),
              label: 'Nom',
              value: lookup.accountHolderName ?? '—',
            ),
            _BeneficiaryDetail(
              key: const Key('momo-beneficiary-search-country'),
              label: 'Pays',
              value: lookup.countryCode,
            ),
            _BeneficiaryDetail(
              key: const Key('momo-beneficiary-search-operator'),
              label: 'Opérateur',
              value: operator?.displayName ?? lookup.operator ?? '—',
            ),
            _BeneficiaryDetail(
              key: const Key('momo-beneficiary-search-phone'),
              label: 'Numéro',
              value: lookup.normalizedPhoneNumber,
            ),
            if (onContinue != null) ...[
              const SizedBox(height: 16),
              FilledButton(
                key: const Key('momo-beneficiary-search-continue'),
                onPressed: () => onContinue!(result),
                child: const Text('Continuer'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _ManualEntryRequiredView extends StatelessWidget {
  const _ManualEntryRequiredView({
    required this.result,
    required this.onManualEntry,
  });

  final MobileMoneyBeneficiarySearchResult? result;
  final VoidCallback? onManualEntry;

  @override
  Widget build(BuildContext context) {
    return Card(
      key: const Key('momo-beneficiary-search-manual-required'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Informations à compléter',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 8),
            const Text(
              'AfrikaWallet n’a pas pu confirmer automatiquement toutes les '
              'informations de ce bénéficiaire.',
            ),
            if (result != null) ...[
              const SizedBox(height: 12),
              _BeneficiaryDetail(
                label: 'Pays',
                value: result!.lookup.countryCode,
              ),
              _BeneficiaryDetail(
                label: 'Numéro',
                value: result!.lookup.normalizedPhoneNumber,
              ),
            ],
            if (onManualEntry != null) ...[
              const SizedBox(height: 16),
              OutlinedButton(
                key: const Key(
                  'momo-beneficiary-search-manual-required-action',
                ),
                onPressed: onManualEntry,
                child: const Text('Saisir manuellement'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _FailedView extends StatelessWidget {
  const _FailedView();

  @override
  Widget build(BuildContext context) {
    return const Card(
      key: Key('momo-beneficiary-search-failed'),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          children: [
            Icon(Icons.cloud_off_outlined, size: 44),
            SizedBox(height: 12),
            Text(
              'Recherche indisponible',
              style: TextStyle(fontWeight: FontWeight.w600),
            ),
            SizedBox(height: 8),
            Text(
              'Le bénéficiaire n’a pas pu être vérifié. '
              'Vérifiez le numéro puis réessayez.',
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

class _BeneficiaryDetail extends StatelessWidget {
  const _BeneficiaryDetail({
    super.key,
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Text(label)),
          const SizedBox(width: 16),
          Flexible(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }
}
