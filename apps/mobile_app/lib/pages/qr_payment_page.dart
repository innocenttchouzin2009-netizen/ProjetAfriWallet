import 'package:flutter/material.dart';

import '../models/qr_payment.dart';
import '../services/afriwallet_qr_decoder.dart';
import '../services/qr_payment_repository.dart';

class QrPaymentPage extends StatefulWidget {
  const QrPaymentPage({
    super.key,
    required this.repository,
    required this.onContinue,
    this.decoder = const AfriWalletQrDecoder(),
  });

  final QrPaymentRepository repository;
  final AfriWalletQrDecoder decoder;
  final VoidCallback onContinue;

  @override
  State<QrPaymentPage> createState() => _QrPaymentPageState();
}

class _QrPaymentPageState extends State<QrPaymentPage> {
  final _controller = TextEditingController();
  QrPaymentPayload? _payload;
  String? _error;
  bool _validating = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _validate() async {
    if (_validating) return;
    setState(() {
      _validating = true;
      _payload = null;
      _error = null;
    });

    try {
      // Beta1.7 keeps parsing deterministic on-device while the repository
      // remains the boundary for authoritative backend validation/payment.
      final payload = widget.decoder.decode(_controller.text);
      if (!mounted) return;
      setState(() => _payload = payload);
    } on InvalidQrPaymentException catch (error) {
      if (!mounted) return;
      setState(() => _error = error.message);
    } catch (_) {
      if (!mounted) return;
      setState(() => _error = 'Impossible de valider ce QR AfriWallet.');
    } finally {
      if (mounted) setState(() => _validating = false);
    }
  }

  String _formatAmount(QrPaymentPayload payload) {
    if (payload.type == QrPaymentType.dynamic && payload.amountMinor == 0) {
      return 'Montant à définir';
    }
    final major = payload.amountMinor ~/ 100;
    final minor = (payload.amountMinor % 100).toString().padLeft(2, '0');
    return '$major.$minor ${payload.currencyCode}';
  }

  @override
  Widget build(BuildContext context) {
    final payload = _payload;
    return Scaffold(
      appBar: AppBar(title: const Text('QR Payments')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const Icon(Icons.qr_code_scanner, size: 64),
            const SizedBox(height: 16),
            Text(
              'Scanner → Validation → Review',
              style: Theme.of(context).textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            const Text(
              'Aucun paiement n’est exécuté au scan. Vérifiez toujours le marchand, le montant et la devise avant de confirmer.',
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            TextField(
              controller: _controller,
              decoration: const InputDecoration(
                labelText: 'QR AfriWallet',
                hintText: 'AFW|Static|merchant-001|15.50|XAF|...',
                border: OutlineInputBorder(),
              ),
              minLines: 2,
              maxLines: 3,
            ),
            const SizedBox(height: 12),
            FilledButton.icon(
              onPressed: _validating ? null : _validate,
              icon: _validating
                  ? const SizedBox.square(
                      dimension: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.verified_outlined),
              label: const Text('Valider le QR'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 16),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text('QR invalide : $_error'),
                ),
              ),
            ],
            if (payload != null) ...[
              const SizedBox(height: 24),
              Text('Vérifier avant paiement', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 12),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        payload.merchantName.isEmpty ? 'Marchand AfriWallet' : payload.merchantName,
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      const SizedBox(height: 6),
                      Text('Merchant ID : ${payload.merchantId}'),
                      const Divider(height: 28),
                      Text(
                        _formatAmount(payload),
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                      Text('Devise : ${payload.currencyCode}'),
                      if (payload.description.isNotEmpty) ...[
                        const SizedBox(height: 12),
                        Text(payload.description),
                      ],
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 12),
              const Text(
                'QR validé localement. La confirmation financière devra provenir du backend AfriWallet.',
              ),
            ],
            const SizedBox(height: 24),
            OutlinedButton(
              onPressed: widget.onContinue,
              child: const Text('Continuer'),
            ),
          ],
        ),
      ),
    );
  }
}
