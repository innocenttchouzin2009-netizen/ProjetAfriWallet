import 'package:flutter/material.dart';

import '../models/qr_payment.dart';
import '../services/qr_payment_repository.dart';

class QrPaymentReviewPage extends StatefulWidget {
  const QrPaymentReviewPage({
    super.key,
    required this.payload,
    required this.repository,
    required this.payerWalletId,
  });

  final QrPaymentPayload payload;
  final QrPaymentRepository repository;
  final String payerWalletId;

  @override
  State<QrPaymentReviewPage> createState() => _QrPaymentReviewPageState();
}

class _QrPaymentReviewPageState extends State<QrPaymentReviewPage> {
  bool _submitting = false;
  QrPaymentResult? _result;
  String? _error;

  String _formatAmount(int minor, String currency) {
    final whole = minor ~/ 100;
    final fraction = (minor % 100).abs().toString().padLeft(2, '0');
    return '$whole.$fraction $currency';
  }

  Future<void> _confirm() async {
    if (_submitting || widget.payload.isExpired) return;
    setState(() {
      _submitting = true;
      _error = null;
      _result = null;
    });

    try {
      final initiated = await widget.repository.initiatePayment(
        payload: widget.payload,
        payerWalletId: widget.payerWalletId,
      );
      if (!mounted) return;

      final transferIntentId = initiated.transferIntentId;
      if (transferIntentId == null || transferIntentId.trim().isEmpty) {
        setState(
          () => _result = const QrPaymentResult(
            status: QrPaymentStatus.pendingConfirmation,
            message: 'Paiement initié, confirmation backend indisponible.',
          ),
        );
        return;
      }

      final authoritative = await widget.repository.getAuthoritativeStatus(
        transferIntentId,
      );
      if (!mounted) return;
      setState(() => _result = authoritative);
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final payload = widget.payload;
    final merchant = payload.merchantName.isEmpty
        ? payload.merchantId
        : payload.merchantName;
    final dynamicAmountRequired =
        payload.type == QrPaymentType.dynamic && payload.amountMinor == 0;

    return Scaffold(
      appBar: AppBar(title: const Text('Paiement QR')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
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
                'Scanner un QR ne déclenche jamais un paiement. Vérifiez les informations puis confirmez explicitement.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 32),
              _ReviewRow(label: 'Marchand', value: merchant),
              _ReviewRow(label: 'Identifiant', value: payload.merchantId),
              _ReviewRow(
                label: 'Montant',
                value: dynamicAmountRequired
                    ? 'Montant à définir'
                    : _formatAmount(payload.amountMinor, payload.currencyCode),
              ),
              _ReviewRow(label: 'Devise', value: payload.currencyCode),
              _ReviewRow(
                label: 'Type',
                value: payload.type == QrPaymentType.static
                    ? 'Statique'
                    : 'Dynamique',
              ),
              if (payload.description.isNotEmpty)
                _ReviewRow(label: 'Description', value: payload.description),
              if (payload.isExpired) ...[
                const SizedBox(height: 16),
                const Text(
                  'Ce QR a expiré. Le paiement ne peut pas être confirmé.',
                  key: Key('qr-expired'),
                  textAlign: TextAlign.center,
                ),
              ],
              if (dynamicAmountRequired) ...[
                const SizedBox(height: 16),
                const Text(
                  'QR dynamique : le montant doit être défini avant confirmation.',
                  key: Key('qr-dynamic-amount-required'),
                  textAlign: TextAlign.center,
                ),
              ],
              const SizedBox(height: 32),
              FilledButton.icon(
                key: const Key('qr-confirm-payment'),
                onPressed:
                    _submitting || payload.isExpired || dynamicAmountRequired
                    ? null
                    : _confirm,
                icon: _submitting
                    ? const SizedBox.square(
                        dimension: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.lock_outline),
                label: Text(
                  _submitting ? 'Vérification…' : 'Confirmer le paiement',
                ),
              ),
              const SizedBox(height: 12),
              TextButton(
                onPressed: _submitting
                    ? null
                    : () => Navigator.of(context).maybePop(),
                child: const Text('Annuler'),
              ),
              if (_error != null) ...[
                const SizedBox(height: 16),
                Text(_error!, key: const Key('qr-payment-error')),
              ],
              if (_result != null) ...[
                const SizedBox(height: 16),
                _PaymentStatusCard(result: _result!),
              ],
              const SizedBox(height: 16),
              const Text(
                'AfriWallet n’affichera « payé » qu’après confirmation financière autoritaire du backend.',
                textAlign: TextAlign.center,
              ),
            ],
          ),
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

class _PaymentStatusCard extends StatelessWidget {
  const _PaymentStatusCard({required this.result});

  final QrPaymentResult result;

  @override
  Widget build(BuildContext context) {
    final confirmed = result.isFinanciallyConfirmed;
    return Card(
      key: const Key('qr-payment-result'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              confirmed
                  ? 'Paiement confirmé par le backend'
                  : 'Paiement non confirmé',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            if (result.message != null) ...[
              const SizedBox(height: 8),
              Text(result.message!),
            ],
            if (confirmed && result.receiptCode != null) ...[
              const SizedBox(height: 8),
              Text('Reçu : ${result.receiptCode}'),
            ],
          ],
        ),
      ),
    );
  }
}
