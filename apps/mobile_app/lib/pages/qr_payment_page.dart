import 'package:flutter/material.dart';

import '../models/qr_payment.dart';
import '../services/afriwallet_qr_decoder.dart';
import '../services/qr_payment_repository.dart';
import 'qr_payment_review_page.dart';
import 'qr_scanner_page.dart';

class QrPaymentPage extends StatefulWidget {
  const QrPaymentPage({
    super.key,
    required this.repository,
    required this.onContinue,
    this.onReturnToWallet,
    this.decoder = const AfriWalletQrDecoder(),
    this.payerWalletId = 'CURRENT-AUTHENTICATED-WALLET',
  });

  final QrPaymentRepository repository;
  final AfriWalletQrDecoder decoder;
  final VoidCallback onContinue;
  final VoidCallback? onReturnToWallet;
  final String payerWalletId;

  @override
  State<QrPaymentPage> createState() => _QrPaymentPageState();
}

class _QrPaymentPageState extends State<QrPaymentPage> {
  final _controller = TextEditingController();
  String? _error;
  bool _validating = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openReview(QrPaymentPayload payload) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute<void>(
        builder: (_) => QrPaymentReviewPage(
          payload: payload,
          repository: widget.repository,
          payerWalletId: widget.payerWalletId,
        ),
      ),
    );
  }

  Future<void> _openScanner() async {
    final payload = await Navigator.of(context).push<QrPaymentPayload>(
      MaterialPageRoute<QrPaymentPayload>(
        builder: (_) => QrScannerPage(decoder: widget.decoder),
      ),
    );
    if (!mounted || payload == null) return;
    await _openReview(payload);
  }

  Future<void> _validateTestInput() async {
    if (_validating) return;
    setState(() {
      _validating = true;
      _error = null;
    });

    try {
      final payload = widget.decoder.decode(_controller.text);
      if (!mounted) return;
      await _openReview(payload);
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

  @override
  Widget build(BuildContext context) {
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
              'Aucun paiement n’est exécuté au scan. Un QR valide ouvre toujours l’écran de vérification avant toute confirmation.',
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            FilledButton.icon(
              key: const Key('open-qr-scanner'),
              onPressed: _openScanner,
              icon: const Icon(Icons.camera_alt_outlined),
              label: const Text('Scanner avec la caméra'),
            ),
            const SizedBox(height: 20),
            const Divider(),
            const SizedBox(height: 12),
            TextField(
              key: const Key('qr-test-input'),
              controller: _controller,
              decoration: const InputDecoration(
                labelText: 'QR AfriWallet — saisie de test',
                hintText: 'AFW|Static|merchant-001|15.50|XAF|...',
                border: OutlineInputBorder(),
              ),
              minLines: 2,
              maxLines: 3,
            ),
            const SizedBox(height: 12),
            FilledButton.tonalIcon(
              key: const Key('validate-qr-test-input'),
              onPressed: _validating ? null : _validateTestInput,
              icon: _validating
                  ? const SizedBox.square(
                      dimension: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.verified_outlined),
              label: const Text('Valider la saisie de test'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 16),
              Card(
                key: const Key('qr-validation-error'),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text('QR invalide : $_error'),
                ),
              ),
            ],
            if (widget.onReturnToWallet != null) ...[
              const SizedBox(height: 24),
              FilledButton.icon(
                key: const Key('qr-return-to-wallet'),
                onPressed: widget.onReturnToWallet,
                icon: const Icon(Icons.account_balance_wallet_outlined),
                label: const Text('Retour au portefeuille'),
              ),
            ],
            const SizedBox(height: 12),
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
