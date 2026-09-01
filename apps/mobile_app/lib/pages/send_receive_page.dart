import 'package:flutter/material.dart';

import '../models/payment_transfer.dart';
import '../services/transfer_repository.dart';

class SendReceivePage extends StatefulWidget {
  const SendReceivePage({super.key, required this.repository, this.onContinue});

  final TransferRepository repository;
  final VoidCallback? onContinue;

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

  @override
  void dispose() {
    _payeeController.dispose();
    _amountController.dispose();
    _currencyController.dispose();
    super.dispose();
  }

  Future<void> _send() async {
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
        payerId: 'CURRENT-AUTHENTICATED-USER',
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
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Envoyer & Recevoir'),
          bottom: const TabBar(tabs: [Tab(text: 'Envoyer'), Tab(text: 'Recevoir')]),
        ),
        body: TabBarView(children: [_buildSend(context), _ReceiveTab(repository: widget.repository)]),
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
        TextField(controller: _payeeController, decoration: const InputDecoration(labelText: 'AWID ou identifiant destinataire')),
        const SizedBox(height: 12),
        TextField(controller: _amountController, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: const InputDecoration(labelText: 'Montant')),
        const SizedBox(height: 12),
        TextField(controller: _currencyController, decoration: const InputDecoration(labelText: 'Devise (EUR, XAF…)')),
        const SizedBox(height: 20),
        FilledButton.icon(onPressed: _submitting ? null : _send, icon: const Icon(Icons.north_east), label: Text(_submitting ? 'Envoi…' : 'Continuer')),
        if (_error != null) ...[
          const SizedBox(height: 16),
          Text(_error!, key: const Key('send-error')),
        ],
        if (_receipt != null) ...[
          const SizedBox(height: 16),
          Card(child: Padding(padding: const EdgeInsets.all(16), child: Text('Payment Intent ${_receipt!.paymentIntentId}\nÉtat: ${_receipt!.status.name}'))),
        ],
        if (widget.onContinue != null) ...[
          const SizedBox(height: 20),
          TextButton(onPressed: widget.onContinue, child: const Text('Continuer')),
        ],
      ],
    );
  }
}

class _ReceiveTab extends StatelessWidget {
  const _ReceiveTab({required this.repository});
  final TransferRepository repository;

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<ReceiveIdentity>(
      future: repository.loadReceiveIdentity(),
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError || !snapshot.hasData) {
          return const Center(child: Padding(
            padding: EdgeInsets.all(24),
            child: Text('Réception indisponible. Aucun AWID ou QR de paiement n’est simulé.'),
          ));
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
            if (identity.hasBackendQr)
              const Card(child: Padding(padding: EdgeInsets.all(20), child: Center(child: Text('QR backend disponible'))))
            else
              const Text('QR indisponible : aucun jeton QR backend valide n’a été fourni.'),
          ],
        );
      },
    );
  }
}
