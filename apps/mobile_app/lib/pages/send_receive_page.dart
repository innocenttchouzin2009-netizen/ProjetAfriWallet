import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../models/payment_transfer.dart';
import '../services/transfer_repository.dart';
import 'p2p_qr_scanner_page.dart';

enum SendReceiveMode { send, receive }

typedef P2pQrScanner = Future<String?> Function(BuildContext context);

class SendReceivePage extends StatefulWidget {
  const SendReceivePage({
    super.key,
    required this.repository,
    this.sourceWalletId,
    this.initialMode = SendReceiveMode.send,
    this.onContinue,
    this.onReturnToWallet,
    this.p2pQrScanner,
  });

  final TransferRepository repository;
  final String? sourceWalletId;
  final SendReceiveMode initialMode;
  final VoidCallback? onContinue;
  final VoidCallback? onReturnToWallet;
  final P2pQrScanner? p2pQrScanner;

  @override
  State<SendReceivePage> createState() => _SendReceivePageState();
}

class _SendReceivePageState extends State<SendReceivePage> {
  final _payeeController = TextEditingController();
  final _amountController = TextEditingController();
  final _currencyController = TextEditingController(text: 'EUR');
  bool _submitting = false;
  TransferReceipt? _receipt;
  String? _error;
  TransferRecipientKind _recipientKind = TransferRecipientKind.afWalId;

  @override
  void dispose() {
    _payeeController.dispose();
    _amountController.dispose();
    _currencyController.dispose();
    super.dispose();
  }

  Future<void> _scanP2pQr() async {
    final scanner = widget.p2pQrScanner ?? _openDefaultP2pScanner;
    final token = (await scanner(context))?.trim();
    if (!mounted || token == null || token.isEmpty) return;

    setState(() {
      _recipientKind = TransferRecipientKind.qr;
      _payeeController.text = token;
      _error = null;
    });
  }

  Future<String?> _openDefaultP2pScanner(BuildContext context) {
    return Navigator.of(context).push<String>(
      MaterialPageRoute<String>(builder: (_) => const P2pQrScannerPage()),
    );
  }

  void _selectRecipientKind(TransferRecipientKind kind) {
    setState(() {
      _recipientKind = kind;
      _payeeController.clear();
      _error = null;
      _receipt = null;
    });
  }

  Future<void> _send() async {
    final sourceWalletId = widget.sourceWalletId?.trim();
    if (sourceWalletId == null || sourceWalletId.isEmpty) {
      setState(() => _error = 'Sélectionnez un portefeuille source avant l’envoi.');
      return;
    }

    final payee = _payeeController.text.trim();
    final amount = double.tryParse(_amountController.text.replaceAll(',', '.'));
    final currency = _currencyController.text.trim().toUpperCase();
    if (payee.isEmpty || amount == null || amount <= 0 || currency.length != 3) {
      setState(() => _error = 'Vérifiez le destinataire, le montant et la devise.');
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
      _receipt = null;
    });
    try {
      final receipt = await widget.repository.send(SendTransferRequest(
        sourceWalletId: sourceWalletId,
        recipientKind: _recipientKind,
        payeeId: payee,
        amountMinor: (amount * 100).round(),
        currencyCode: currency,
        idempotencyKey: DateTime.now().microsecondsSinceEpoch.toString(),
      ));
      if (!mounted) return;
      setState(() => _receipt = receipt);
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      initialIndex: widget.initialMode == SendReceiveMode.send ? 0 : 1,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Envoyer & Recevoir'),
          bottom: const TabBar(tabs: [Tab(text: 'Envoyer'), Tab(text: 'Recevoir')]),
        ),
        body: TabBarView(
          children: [
            _buildSend(context),
            _ReceiveTab(
              repository: widget.repository,
              onReturnToWallet: widget.onReturnToWallet,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSend(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Envoyer de l’argent', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 8),
        const Text('Le transfert est créé uniquement par un service backend autorisé.'),
        const SizedBox(height: 20),
        SegmentedButton<TransferRecipientKind>(
          segments: const [
            ButtonSegment(
              value: TransferRecipientKind.afWalId,
              label: Text('AfWal ID'),
              icon: Icon(Icons.alternate_email),
            ),
            ButtonSegment(
              value: TransferRecipientKind.qr,
              label: Text('QR'),
              icon: Icon(Icons.qr_code_scanner),
            ),
          ],
          selected: {_recipientKind},
          onSelectionChanged: (selection) => _selectRecipientKind(selection.single),
        ),
        const SizedBox(height: 16),
        if (_recipientKind == TransferRecipientKind.afWalId)
          TextField(
            key: const Key('p2p-recipient-input'),
            controller: _payeeController,
            decoration: const InputDecoration(labelText: 'AfWal ID ou identifiant destinataire'),
          )
        else ...[
          TextField(
            key: const Key('p2p-qr-token-input'),
            controller: _payeeController,
            readOnly: true,
            decoration: const InputDecoration(labelText: 'Destinataire QR P2P'),
          ),
          const SizedBox(height: 8),
          OutlinedButton.icon(
            key: const Key('scan-p2p-qr'),
            onPressed: _scanP2pQr,
            icon: const Icon(Icons.camera_alt_outlined),
            label: const Text('Scanner un QR P2P'),
          ),
          const SizedBox(height: 8),
          const Text(
            'Scanner un QR ne déclenche jamais un transfert. Vérifiez le montant puis confirmez explicitement.',
          ),
        ],
        const SizedBox(height: 12),
        TextField(
          controller: _amountController,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(labelText: 'Montant'),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _currencyController,
          decoration: const InputDecoration(labelText: 'Devise (EUR, XAF…)'),
        ),
        const SizedBox(height: 20),
        FilledButton.icon(
          onPressed: _submitting ? null : _send,
          icon: const Icon(Icons.north_east),
          label: Text(_submitting ? 'Envoi…' : 'Continuer'),
        ),
        if (_error != null) ...[
          const SizedBox(height: 16),
          Text(_error!, key: const Key('send-error')),
        ],
        if (_receipt != null) ...[
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Text('Payment Intent ${_receipt!.paymentIntentId}\nÉtat: ${_receipt!.status.name}'),
            ),
          ),
        ],
        if (widget.onReturnToWallet != null) ...[
          const SizedBox(height: 20),
          TextButton.icon(
            key: const Key('return-to-wallet-send'),
            onPressed: widget.onReturnToWallet,
            icon: const Icon(Icons.account_balance_wallet_outlined),
            label: const Text('Retour au portefeuille'),
          ),
        ],
        if (widget.onContinue != null) ...[
          const SizedBox(height: 8),
          TextButton(onPressed: widget.onContinue, child: const Text('Continuer')),
        ],
      ],
    );
  }
}

class _ReceiveTab extends StatelessWidget {
  const _ReceiveTab({required this.repository, this.onReturnToWallet});

  final TransferRepository repository;
  final VoidCallback? onReturnToWallet;

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<ReceiveIdentity>(
      future: repository.loadReceiveIdentity(),
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError || !snapshot.hasData) {
          return Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text('Réception indisponible. Aucun AfWal ID ou QR de paiement n’est simulé.'),
                  if (onReturnToWallet != null) ...[
                    const SizedBox(height: 20),
                    TextButton.icon(
                      key: const Key('return-to-wallet-receive-unavailable'),
                      onPressed: onReturnToWallet,
                      icon: const Icon(Icons.account_balance_wallet_outlined),
                      label: const Text('Retour au portefeuille'),
                    ),
                  ],
                ],
              ),
            ),
          );
        }
        final identity = snapshot.data!;
        return ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Text('Recevoir de l’argent', style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 16),
            const Text('Votre identité publique de réception'),
            const SizedBox(height: 8),
            SelectableText(identity.publicLabel, key: const Key('receive-public-label')),
            const SizedBox(height: 20),
            if (identity.hasBackendQr) ...[
              Card(
                key: const Key('receive-p2p-qr-card'),
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    children: [
                      QrImageView(
                        key: const Key('receive-p2p-qr'),
                        data: identity.qrToken!,
                        size: 220,
                      ),
                      const SizedBox(height: 12),
                      const Text(
                        'Ce QR contient uniquement votre identité de réception P2P fournie par le backend.',
                        textAlign: TextAlign.center,
                      ),
                    ],
                  ),
                ),
              ),
            ] else
              const Text('QR indisponible : aucun jeton QR backend valide n’a été fourni.'),
            if (onReturnToWallet != null) ...[
              const SizedBox(height: 20),
              TextButton.icon(
                key: const Key('return-to-wallet-receive'),
                onPressed: onReturnToWallet,
                icon: const Icon(Icons.account_balance_wallet_outlined),
                label: const Text('Retour au portefeuille'),
              ),
            ],
          ],
        );
      },
    );
  }
}
