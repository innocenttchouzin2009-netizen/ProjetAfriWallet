import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

class P2pQrScannerPage extends StatefulWidget {
  const P2pQrScannerPage({super.key});

  @override
  State<P2pQrScannerPage> createState() => _P2pQrScannerPageState();
}

class _P2pQrScannerPageState extends State<P2pQrScannerPage> {
  final MobileScannerController _controller = MobileScannerController(
    formats: const [BarcodeFormat.qrCode],
    detectionSpeed: DetectionSpeed.noDuplicates,
  );

  bool _handling = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _handleCapture(BarcodeCapture capture) async {
    if (_handling || capture.barcodes.isEmpty) return;
    final token = capture.barcodes.first.rawValue?.trim();
    if (token == null || token.isEmpty) return;

    _handling = true;
    await _controller.stop();
    if (!mounted) return;
    Navigator.of(context).pop<String>(token);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Scanner un QR P2P'),
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
          MobileScanner(controller: _controller, onDetect: _handleCapture),
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
              child: const Text(
                'Le scan sélectionne uniquement le destinataire P2P. Aucun transfert n’est exécuté automatiquement.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Colors.white),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
