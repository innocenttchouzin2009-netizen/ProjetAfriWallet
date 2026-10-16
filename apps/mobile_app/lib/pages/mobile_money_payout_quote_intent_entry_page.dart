import 'package:flutter/material.dart';

import '../models/mobile_money_payout.dart';
import '../models/mobile_money_payout_quote.dart';
import '../models/mobile_money_payout_quote_intent.dart';
import '../models/wallet_balance.dart';
import '../presentation/mobile_money_payout_funding_selection_controller.dart';
import '../presentation/mobile_money_payout_quote_controller.dart';
import '../services/mobile_money_beneficiary_lookup_repository.dart';
import '../services/mobile_money_payout_quote_intent_mapper.dart';
import '../services/mobile_money_payout_quote_repository.dart';
import 'mobile_money_payout_funding_selection_page.dart';
import 'mobile_money_payout_quote_page.dart';

class MobileMoneyPayoutQuoteIntentEntryPage extends StatefulWidget {
  const MobileMoneyPayoutQuoteIntentEntryPage({
    super.key,
    required this.sourceWallet,
    required this.beneficiary,
    required this.quoteRepository,
    this.onBack,
  });

  final WalletBalance sourceWallet;
  final MobileMoneyBeneficiarySearchResult beneficiary;
  final MobileMoneyPayoutQuoteRepository quoteRepository;
  final VoidCallback? onBack;

  @override
  State<MobileMoneyPayoutQuoteIntentEntryPage> createState() =>
      _MobileMoneyPayoutQuoteIntentEntryPageState();
}

class _MobileMoneyPayoutQuoteIntentEntryPageState
    extends State<MobileMoneyPayoutQuoteIntentEntryPage> {
  final TextEditingController _amountController = TextEditingController();
  String? _amountError;

  @override
  void dispose() {
    _amountController.dispose();
    super.dispose();
  }

  String? _sourceContextError() {
    final sourceWallet = widget.sourceWallet;
    final sourceCountryCode = sourceWallet.countryCode?.trim().toUpperCase();
    final sourceCurrencyCode = sourceWallet.currency.trim().toUpperCase();

    if (sourceWallet.walletId.trim().isEmpty) {
      return 'L’identifiant du wallet source est indisponible.';
    }
    if (sourceWallet.availableMinor < 0) {
      return 'Le solde disponible du wallet source est invalide.';
    }
    if (!sourceWallet.isAvailable) {
      return 'Le wallet source principal n’est pas disponible.';
    }
    if (sourceCountryCode == null ||
        !RegExp(r'^[A-Z]{2}$').hasMatch(sourceCountryCode)) {
      return 'Le pays du wallet source est indisponible ou invalide.';
    }
    if (!RegExp(r'^[A-Z]{3}$').hasMatch(sourceCurrencyCode)) {
      return 'La devise du wallet source est indisponible ou invalide.';
    }
    if (!widget.beneficiary.isFullyResolved) {
      return 'Le bénéficiaire Mobile Money doit être entièrement vérifié.';
    }
    if (widget.beneficiary.lookup.countryCode.trim().toUpperCase() != 'CM') {
      return 'Ce jalon prend uniquement en charge un bénéficiaire au Cameroun.';
    }
    return null;
  }

  void _continueToQuote() {
    final sourceError = _sourceContextError();
    if (sourceError != null) {
      setState(() => _amountError = sourceError);
      return;
    }

    final amountMinor = _parseMinorUnits(_amountController.text);
    if (amountMinor == null || amountMinor <= 0) {
      setState(() {
        _amountError =
            'Saisissez un montant strictement positif avec au maximum deux décimales.';
      });
      return;
    }

    try {
      final beneficiary =
          MobileMoneyPayoutQuoteIntentMapper.beneficiaryFromSearchResult(
        widget.beneficiary,
      );
      final intent = MobileMoneyPayoutQuoteIntent(
        sourceCountryCode: widget.sourceWallet.countryCode!.trim().toUpperCase(),
        sourceCurrencyCode: widget.sourceWallet.currency.trim().toUpperCase(),
        destinationCurrencyCode: 'XAF',
        sourceAmountMinor: amountMinor,
        beneficiary: beneficiary,
      );
      intent.validate();

      setState(() => _amountError = null);
      final controller = MobileMoneyPayoutQuoteController(
        repository: widget.quoteRepository,
      );
      Navigator.of(context)
          .push<void>(
            MaterialPageRoute<void>(
              builder: (quoteContext) => MobileMoneyPayoutQuotePage(
                controller: controller,
                intent: intent,
                onContinue: (quote) =>
                    _openFundingSelection(quoteContext, quote),
                onBack: () => Navigator.of(quoteContext).pop(),
              ),
            ),
          )
          .whenComplete(controller.dispose);
    } on Object {
      setState(() {
        _amountError =
            'Impossible de préparer ce devis avec les données disponibles.';
      });
    }
  }


  void _openFundingSelection(
    BuildContext quoteContext,
    MobileMoneyPayoutQuote quote,
  ) {
    final sourceError = _sourceContextError();
    final sourceWallet = widget.sourceWallet;
    final sourceCurrencyCode = sourceWallet.currency.trim().toUpperCase();
    final quoteCurrencyCode = quote.sourceCurrencyCode.trim().toUpperCase();

    if (sourceError != null || quoteCurrencyCode != sourceCurrencyCode) {
      ScaffoldMessenger.of(quoteContext).showSnackBar(
        const SnackBar(
          content: Text(
            'Le wallet source ne peut pas financer ce devis.',
            key: Key('momo-funding-source-invalid-snackbar'),
          ),
        ),
      );
      return;
    }

    try {
      final walletSource = FundingSource(
        id: sourceWallet.walletId,
        type: FundingSourceType.wallet,
        displayLabel: 'Solde AfrikaWallet',
        currencyCode: sourceCurrencyCode,
        isAvailable: sourceWallet.isAvailable,
        availableMinor: sourceWallet.availableMinor,
      );
      walletSource.validate();

      final controller = MobileMoneyPayoutFundingSelectionController(
        quote: quote,
        fundingSources: <FundingSource>[walletSource],
      );

      Navigator.of(quoteContext)
          .push<void>(
            MaterialPageRoute<void>(
              builder: (fundingContext) =>
                  MobileMoneyPayoutFundingSelectionPage(
                controller: controller,
                onBack: () => Navigator.of(fundingContext).pop(),
              ),
            ),
          )
          .whenComplete(controller.dispose);
    } on Object {
      ScaffoldMessenger.of(quoteContext).showSnackBar(
        const SnackBar(
          content: Text(
            'Le wallet source ne peut pas financer ce devis.',
            key: Key('momo-funding-source-invalid-snackbar'),
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final sourceError = _sourceContextError();
    final sourceCountryCode =
        widget.sourceWallet.countryCode?.trim().toUpperCase() ?? '—';
    final sourceCurrencyCode = widget.sourceWallet.currency.trim().toUpperCase();
    final lookup = widget.beneficiary.lookup;

    return Scaffold(
      key: const Key('momo-quote-intent-entry-page'),
      appBar: AppBar(
        title: const Text('Montant du transfert'),
        leading: widget.onBack == null
            ? null
            : IconButton(
                key: const Key('momo-quote-intent-back'),
                onPressed: widget.onBack,
                icon: const Icon(Icons.arrow_back),
              ),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Text(
              'Préparer votre devis Mobile Money',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            const Text(
              'Le devis sera calculé à partir du wallet principal déjà affiché '
              'dans votre accueil et du bénéficiaire vérifié.',
            ),
            const SizedBox(height: 20),
            Card(
              key: const Key('momo-quote-intent-source'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Wallet source'),
                    const SizedBox(height: 8),
                    Text(widget.sourceWallet.walletId),
                    Text('$sourceCountryCode · $sourceCurrencyCode'),
                    Text(widget.sourceWallet.formattedAmount),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),
            Card(
              key: const Key('momo-quote-intent-beneficiary'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Bénéficiaire'),
                    const SizedBox(height: 8),
                    Text(lookup.accountHolderName ?? '—'),
                    Text(lookup.normalizedPhoneNumber),
                    Text(
                      '${lookup.countryCode} · '
                      '${widget.beneficiary.operator?.displayName ?? '—'}',
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 20),
            if (sourceError != null) ...[
              Card(
                key: const Key('momo-quote-intent-source-unavailable'),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text(sourceError),
                ),
              ),
              const SizedBox(height: 16),
            ],
            TextField(
              key: const Key('momo-quote-intent-amount'),
              controller: _amountController,
              enabled: sourceError == null,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(
                labelText: 'Montant à envoyer ($sourceCurrencyCode)',
                border: const OutlineInputBorder(),
                errorText: _amountError,
              ),
              onSubmitted:
                  sourceError == null ? (_) => _continueToQuote() : null,
            ),
            const SizedBox(height: 16),
            FilledButton(
              key: const Key('momo-quote-intent-continue'),
              onPressed: sourceError == null ? _continueToQuote : null,
              child: const Text('Obtenir le devis'),
            ),
          ],
        ),
      ),
    );
  }
}

int? _parseMinorUnits(String raw) {
  final normalized = raw.trim().replaceAll(' ', '').replaceAll(',', '.');
  final match =
      RegExp(r'^([0-9]+)(?:\.([0-9]{1,2}))?$').firstMatch(normalized);
  if (match == null) return null;

  final major = int.tryParse(match.group(1)!);
  if (major == null) return null;
  final fraction = (match.group(2) ?? '').padRight(2, '0');
  final minor = fraction.isEmpty ? 0 : int.tryParse(fraction);
  if (minor == null) return null;
  return major * 100 + minor;
}
