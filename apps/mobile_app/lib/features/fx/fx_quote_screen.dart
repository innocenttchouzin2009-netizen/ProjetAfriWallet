import 'dart:async';
import 'package:flutter/material.dart';
import '../wallets/wallet_service.dart';
import '../../l10n/app_localizations.dart';

class FxQuoteScreen extends StatefulWidget {
  const FxQuoteScreen({super.key, this.apiClient});

  final WalletApiClient? apiClient;

  @override
  State<FxQuoteScreen> createState() => _FxQuoteScreenState();
}

class _FxQuoteScreenState extends State<FxQuoteScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amountController = TextEditingController(text: '1000');
  String _fromCurrency = 'EUR';
  String _toCurrency = 'USD';
  Map<String, dynamic>? _quote;
  Timer? _timer;
  int _secondsLeft = 0;
  bool _isExpired = false;

  @override
  void dispose() {
    _timer?.cancel();
    _amountController.dispose();
    super.dispose();
  }

  Future<void> _createQuote() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final service = widget.apiClient ?? WalletApiClient();
    final quote = await service.createFxQuote(
      from: _fromCurrency,
      to: _toCurrency,
      amountMinor: int.parse(_amountController.text),
    );
    setState(() {
      _quote = quote;
      _secondsLeft = quote['expiresInSeconds'] as int? ?? 0;
      _isExpired = _secondsLeft <= 0;
    });
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) return;
      setState(() {
        if (_secondsLeft > 0) {
          _secondsLeft -= 1;
        }
        _isExpired = _secondsLeft <= 0;
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context) ?? _FallbackLocalizations();
    return Scaffold(
      appBar: AppBar(title: Text(l10n.fxQuote)),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: ListView(
            children: [
              TextFormField(
                controller: _amountController,
                keyboardType: TextInputType.number,
                decoration: InputDecoration(labelText: l10n.sourceAmount),
                validator: (value) => (value == null || value.isEmpty) ? l10n.requiredField : null,
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      initialValue: _fromCurrency,
                      decoration: InputDecoration(labelText: l10n.fromCurrency),
                      onChanged: (value) => _fromCurrency = value.toUpperCase(),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextFormField(
                      initialValue: _toCurrency,
                      decoration: InputDecoration(labelText: l10n.toCurrency),
                      onChanged: (value) => _toCurrency = value.toUpperCase(),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: _createQuote,
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.currency_exchange),
                    const SizedBox(width: 8),
                    Text(l10n.getQuote),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              if (_quote != null) ...[
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(l10n.quoteSummary, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                        const SizedBox(height: 8),
                        Text('${l10n.sourceAmount}: ${_quote!['sourceAmountMinor']}'),
                        Text('${l10n.amountReceived}: ${_quote!['targetAmountMinor']}'),
                        Text('${l10n.marketRate}: ${_quote!['exchangeRate']}'),
                        Text('${l10n.appliedRate}: ${_quote!['appliedRate']}'),
                        Text('${l10n.spread}: ${_quote!['spread']}'),
                        Text('${l10n.fees}: ${_quote!['fee']}'),
                        Text('${l10n.countdown}: ${_secondsLeft}s'),
                        Text('${l10n.expiresAt}: ${_quote!['expiresAt']}'),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                ElevatedButton(
                  onPressed: _isExpired ? null : () {},
                  child: const Text('Confirm quote'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _FallbackLocalizations extends AppLocalizations {
  _FallbackLocalizations() : super('en');

  @override
  String get wallets => 'Wallets';

  @override
  String get createWallet => 'Create wallet';

  @override
  String get fxQuote => 'FX Quote';

  @override
  String get sourceAmount => 'Source amount';

  @override
  String get amountReceived => 'Amount received';

  @override
  String get marketRate => 'Market rate';

  @override
  String get appliedRate => 'Applied rate';

  @override
  String get spread => 'Spread';

  @override
  String get fees => 'Fees';

  @override
  String get countdown => 'Countdown';

  @override
  String get expiresAt => 'Expires at';

  @override
  String get fromCurrency => 'From currency';

  @override
  String get toCurrency => 'To currency';

  @override
  String get getQuote => 'Get quote';

  @override
  String get quoteSummary => 'Quote summary';

  @override
  String get requiredField => 'This field is required';
}
