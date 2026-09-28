import 'package:flutter/material.dart';

import '../models/payment_transfer.dart';
import '../services/transfer_repository.dart';

class P2pTransferReviewPage extends StatefulWidget {
  const P2pTransferReviewPage({
    super.key,
    required this.repository,
    required this.request,
  });

  final TransferRepository repository;
  final SendTransferRequest request;

  @override
  State<P2pTransferReviewPage> createState() => _P2pTransferReviewPageState();
}

class _P2pTransferReviewPageState extends State<P2pTransferReviewPage> {
  bool _submitting = false;
  String? _error;
  TransferReceipt? _receipt;

  Future<void> _confirm() async {
    if (_submitting || _receipt != null) return;

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      final receipt = await widget.repository.send(widget.request);
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
    final request = widget.request;
    final amount = (request.amountMinor / 100).toStringAsFixed(2);
    final recipientType = switch (request.recipientKind) {
      TransferRecipientKind.afWalId => 'AfWal ID',
      TransferRecipientKind.qr => 'QR P2P',
    };

    return Scaffold(
      appBar: AppBar(title: const Text('Vérifier le transfert')),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Text(
            'Vérifiez avant de confirmer',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'Aucun transfert n’est exécuté avant votre confirmation explicite.',
          ),
          const SizedBox(height: 24),
          _ReviewRow(label: 'Portefeuille source', value: request.sourceWalletId),
          _ReviewRow(label: 'Type destinataire', value: recipientType),
          _ReviewRow(label: 'Destinataire', value: request.payeeId),
          _ReviewRow(
            label: 'Montant',
            value: '${amount} ${request.currencyCode}',
          ),
          const SizedBox(height: 24),
          FilledButton.icon(
            key: const Key('confirm-p2p-transfer'),
            onPressed: _submitting || _receipt != null ? null : _confirm,
            icon: const Icon(Icons.verified_outlined),
            label: Text(_submitting ? 'Confirmation…' : 'Confirmer le transfert'),
          ),
          const SizedBox(height: 12),
          OutlinedButton(
            key: const Key('edit-p2p-transfer'),
            onPressed: _submitting || _receipt != null
                ? null
                : () => Navigator.of(context).pop(),
            child: const Text('Modifier'),
          ),
          if (_error != null) ...[
            const SizedBox(height: 16),
            Text(_error!, key: const Key('p2p-review-error')),
          ],
          if (_receipt != null) ...[
            const SizedBox(height: 20),
            Card(
              key: const Key('p2p-transfer-receipt'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  'Payment Intent ${_receipt!.paymentIntentId}\n'
                  'État: ${_receipt!.status.name}',
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _ReviewRow extends StatelessWidget {
  const _ReviewRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: Theme.of(context).textTheme.labelLarge),
          const SizedBox(height: 4),
          SelectableText(value),
        ],
      ),
    );
  }
}
