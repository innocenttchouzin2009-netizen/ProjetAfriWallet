import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../models/mobile_money_payout_quote.dart';
import '../models/mobile_money_payout_quote_intent.dart';
import '../presentation/mobile_money_payout_quote_controller.dart';

class MobileMoneyPayoutQuotePage extends StatefulWidget {
  const MobileMoneyPayoutQuotePage({
    super.key,
    required this.controller,
    required this.intent,
    this.onContinue,
    this.onBack,
  });

  final MobileMoneyPayoutQuoteController controller;
  final MobileMoneyPayoutQuoteIntent intent;
  final ValueChanged<MobileMoneyPayoutQuote>? onContinue;
  final VoidCallback? onBack;

  @override
  State<MobileMoneyPayoutQuotePage> createState() =>
      _MobileMoneyPayoutQuotePageState();
}

class _MobileMoneyPayoutQuotePageState
    extends State<MobileMoneyPayoutQuotePage> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerChanged);
    WidgetsBinding.instance.addPostFrameCallback((_) => _createQuote());
  }

  @override
  void didUpdateWidget(covariant MobileMoneyPayoutQuotePage oldWidget) {
    super.didUpdateWidget(oldWidget);
    final controllerChanged = oldWidget.controller != widget.controller;
    final intentChanged = oldWidget.intent != widget.intent;

    if (controllerChanged) {
      oldWidget.controller.removeListener(_onControllerChanged);
      widget.controller.addListener(_onControllerChanged);
    }

    if (controllerChanged || intentChanged) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _createQuote());
    }
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerChanged);
    super.dispose();
  }

  void _onControllerChanged() {
    if (mounted) {
      setState(() {});
    }
  }

  Future<void> _createQuote() {
    return widget.controller.createQuoteFromIntent(widget.intent);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Estimation du transfert'),
        leading: widget.onBack == null
            ? null
            : IconButton(
                key: const Key('momo-quote-back'),
                onPressed: widget.onBack,
                icon: const Icon(Icons.arrow_back),
              ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: _buildBody(context),
        ),
      ),
    );
  }

  Widget _buildBody(BuildContext context) {
    switch (widget.controller.status) {
      case MobileMoneyPayoutQuotePresentationStatus.idle:
      case MobileMoneyPayoutQuotePresentationStatus.loading:
        return const _LoadingView();
      case MobileMoneyPayoutQuotePresentationStatus.ready:
        final quote = widget.controller.quote;
        if (quote == null) {
          return _FailedView(onRetry: _createQuote);
        }
        return _QuoteView(
          quote: quote,
          onContinue: widget.onContinue,
        );
      case MobileMoneyPayoutQuotePresentationStatus.failed:
        return _FailedView(onRetry: _createQuote);
    }
  }
}

class _LoadingView extends StatelessWidget {
  const _LoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(
      key: Key('momo-quote-loading'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircularProgressIndicator(),
          SizedBox(height: 16),
          Text(
            'Calcul de votre estimation Mobile Money…',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

class _QuoteView extends StatelessWidget {
  const _QuoteView({
    required this.quote,
    required this.onContinue,
  });

  final MobileMoneyPayoutQuote quote;
  final ValueChanged<MobileMoneyPayoutQuote>? onContinue;

  @override
  Widget build(BuildContext context) {
    return ListView(
      key: const Key('momo-quote-ready'),
      children: [
        Text(
          'Votre estimation',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 8),
        const Text(
          'Vérifiez le montant, les frais et le montant reçu avant de continuer.',
        ),
        const SizedBox(height: 24),
        _QuoteRow(
          key: const Key('momo-quote-source-amount'),
          label: 'Vous envoyez',
          value: _formatMinor(
            quote.sourceAmountMinor,
            quote.sourceCurrencyCode,
          ),
        ),
        _QuoteRow(
          key: const Key('momo-quote-fees'),
          label: 'Frais',
          value: _formatMinor(
            quote.totalFeeMinor,
            quote.sourceCurrencyCode,
          ),
        ),
        _QuoteRow(
          key: const Key('momo-quote-total-debit'),
          label: 'Total débité',
          value: _formatMinor(
            quote.totalSourceDebitMinor,
            quote.sourceCurrencyCode,
          ),
        ),
        const Divider(height: 32),
        _QuoteRow(
          key: const Key('momo-quote-destination-amount'),
          label: 'Le bénéficiaire reçoit',
          value: _formatMinor(
            quote.destinationAmountMinor,
            quote.destinationCurrencyCode,
          ),
        ),
        _QuoteRow(
          key: const Key('momo-quote-fx-rate'),
          label: 'Taux indicatif',
          value:
              '1 ${quote.sourceCurrencyCode} = ${quote.fxRate} ${quote.destinationCurrencyCode}',
        ),
        const SizedBox(height: 16),
        Text(
          'Estimation valable jusqu’au '
          '${DateFormat('dd/MM/yyyy HH:mm').format(quote.expiresAtUtc.toLocal())}.',
          key: const Key('momo-quote-expiry'),
        ),
        const SizedBox(height: 12),
        const Text(
          'Aucun transfert n’a encore été lancé.',
          key: Key('momo-quote-no-payout'),
        ),
        if (onContinue != null) ...[
          const SizedBox(height: 24),
          FilledButton(
            key: const Key('momo-quote-continue'),
            onPressed: () => onContinue!(quote),
            child: const Text('Continuer'),
          ),
        ],
      ],
    );
  }
}

class _QuoteRow extends StatelessWidget {
  const _QuoteRow({
    super.key,
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
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

class _FailedView extends StatelessWidget {
  const _FailedView({required this.onRetry});

  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      key: const Key('momo-quote-failed'),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off_outlined, size: 56),
          const SizedBox(height: 16),
          Text(
            'Estimation indisponible',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'L’estimation n’a pas pu être calculée. Aucun transfert n’a été lancé.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          OutlinedButton(
            key: const Key('momo-quote-retry'),
            onPressed: onRetry,
            child: const Text('Réessayer'),
          ),
        ],
      ),
    );
  }
}

String _formatMinor(int amountMinor, String currencyCode) {
  final formatter = NumberFormat.currency(
    name: currencyCode,
    symbol: currencyCode,
  );
  final decimalDigits = formatter.decimalDigits ?? 2;
  final divisor = math.pow(10, decimalDigits);
  return formatter.format(amountMinor / divisor);
}
