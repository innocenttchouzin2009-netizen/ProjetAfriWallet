import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../models/qr_payment.dart';
import '../services/afriwallet_qr_decoder.dart';

class QrScannerPage extends StatefulWidget {
  const QrScannerPage({
    super.key,
    this.decoder = const AfriWalletQrDecoder(),
  });

  final AfriWalletQrDecoder decoder;

  @override
  State<QrScannerPage> createState() => _QrScannerPageState();
}

class _QrScannerPageState extends State<QrScannerPage> {
  final MobileScannerController _controller = MobileScannerController(
    formats: const [BarcodeFormat.qrCode],
    detectionSpeed: DetectionSpeed.noDuplicates,
  );

  bool _handling = false;
  String? _error;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _handleCapture(BarcodeCapture capture) async {
    if (_handling || capture.barcodes.isEmpty) return;
    final rawCode = capture.barcodes.first.rawValue;
    if (rawCode == null || rawCode.trim().isEmpty) return;

    _handling = true;
    try {
      final payload = widget.decoder.decode(rawCode);
      await _controller.stop();
      if (!mounted) return;
      Navigator.of(context).pop<QrPaymentPayload>(payload);
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error.toString());
      await Future<void>.delayed(const Duration(milliseconds: 900));
      _handling = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Scanner un QR AfriWallet'),
        actions: [
          IconButton(
            tooltip: 'Lampe',
            onPressed: _controller.toggleTorch,
            icon: const Icon(Icons.flashlight_on_outlined),
          ),
        ],
      ),
      body: Stack(
        fit: StackFit.expand,
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _handleCapture,
          ),
          IgnorePointer(
            child: Center(
              child: Container(
                width: 260,
                height: 260,
                decoration: BoxDecoration(
                  border: Border.all(width: 3, color: Colors.white),
                  borderRadius: BorderRadius.circular(24),
                ),
              ),
            ),
          ),
          Align(
            alignment: Alignment.bottomCenter,
            child: Container(
              width: double.infinity,
              padding: const EdgeInsets.fromLTRB(24, 20, 24, 32),
              color: Colors.black54,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text(
                    'Placez le QR dans le cadre. Un QR valide ouvre la vérification; aucun paiement n’est déclenché ici.',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: Colors.white),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Text(
                      _error!,
                      key: const Key('qr-scan-error'),
                      textAlign: TextAlign.center,
                      style: const TextStyle(color: Colors.white),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
