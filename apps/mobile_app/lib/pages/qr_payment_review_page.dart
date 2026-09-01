import 'package:flutter/material.dart';

import '../models/qr_payment.dart';

class QrPaymentReviewPage extends StatelessWidget {
  const QrPaymentReviewPage({
    super.key,
    required this.payload,
    required this.onConfirm,
    this.isSubmitting = false,
  });

  final QrPaymentPayload payload;
  final Future<void> Function() onConfirm;
  final bool isSubmitting;

  String _amountLabel() {
    if (payload.type == QrPaymentType.dynamic && payload.amountMinor == 0) {
      return 'Montant à saisir';
    }

    final whole = payload.amountMinor ~/ 100;
    final cents = (payload.amountMinor % 100).toString().padLeft(2, '0');
    return '$whole.$cents ${payload.currencyCode}';
  }

  @override
  Widget build(BuildContext context) {
    final merchantLabel = payload.merchantName.isNotEmpty
        ? payload.merchantName
        : payload.merchantId;

    return Scaffold(
      appBar: AppBar(title: const Text('Paiement QR')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const Icon(Icons.qr_code_2, size: 72),
            const SizedBox(height: 24),
            Text(
              'Vérifiez avant de payer',
              style: Theme.of(context).textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            const Text(
              'Le scan du QR ne déclenche aucun paiement. Vérifiez les informations puis confirmez explicitement.',
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 32),
            _ReviewRow(label: 'Marchand', value: merchantLabel),
            _ReviewRow(label: 'Identifiant', value: payload.merchantId),
            _ReviewRow(label: 'Montant', value: _amountLabel()),
            _ReviewRow(label: 'Devise', value: payload.currencyCode),
            _ReviewRow(
              label: 'Type',
              value: payload.type == QrPaymentType.static ? 'Statique' : 'Dynamique',
            ),
            if (payload.description.isNotEmpty)
              _ReviewRow(label: 'Description', value: payload.description),
            const SizedBox(height: 32),
            FilledButton.icon(
              onPressed: isSubmitting ? null : () => onConfirm(),
              icon: isSubmitting
                  ? const SizedBox.square(
                      dimension: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.lock_outline),
              label: Text(isSubmitting ? 'Traitement…' : 'Confirmer le paiement'),
            ),
            const SizedBox(height: 12),
            TextButton(
              onPressed: isSubmitting ? null : () => Navigator.of(context).maybePop(),
              child: const Text('Annuler'),
            ),
            const SizedBox(height: 16),
            const Text(
              'AfriWallet n’affichera « payé » qu’après confirmation financière du backend.',
              textAlign: TextAlign.center,
            ),
          ],
        ),
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
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child: Text(label, style: Theme.of(context).textTheme.labelLarge),
          ),
          const SizedBox(width: 12),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }
}
