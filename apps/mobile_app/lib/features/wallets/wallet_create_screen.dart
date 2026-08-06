import 'package:flutter/material.dart';
import 'wallet_controller.dart';

class WalletCreateScreen extends StatefulWidget {
  const WalletCreateScreen({super.key, required this.controller});

  final WalletController controller;

  @override
  State<WalletCreateScreen> createState() => _WalletCreateScreenState();
}

class _WalletCreateScreenState extends State<WalletCreateScreen> {
  final _formKey = GlobalKey<FormState>();
  String _walletType = 'Personal';
  String _currency = '';
  final _nameController = TextEditingController();
  final _currencyController = TextEditingController();

  @override
  void dispose() {
    _nameController.dispose();
    _currencyController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Create wallet')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: ListView(
            children: [
              DropdownButtonFormField<String>(
                initialValue: _walletType,
                items: const [
                  DropdownMenuItem(value: 'Personal', child: Text('Personal')),
                  DropdownMenuItem(value: 'Business', child: Text('Business')),
                ],
                onChanged: (value) => setState(() => _walletType = value ?? 'Personal'),
                decoration: const InputDecoration(labelText: 'Wallet type'),
              ),
              const SizedBox(height: 12),
              TextFormField(
                decoration: const InputDecoration(labelText: 'Currency'),
                controller: _currencyController,
                onChanged: (value) {
                  _currency = value.toUpperCase();
                },
                validator: (value) => (value == null || value.isEmpty) ? 'Currency is required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _nameController,
                decoration: const InputDecoration(labelText: 'Optional name'),
              ),
              const SizedBox(height: 24),
              if (widget.controller.error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(widget.controller.error!, style: const TextStyle(color: Colors.red)),
                ),
              FilledButton(
                onPressed: () async {
                  if (!(_formKey.currentState?.validate() ?? false)) return;
                  final success = await widget.controller.createWallet(
                    awid: 'demo-awid',
                    walletType: _walletType,
                    currency: _currency,
                    name: _nameController.text.isEmpty ? null : _nameController.text,
                  );
                  if (success && mounted) {
                    Navigator.of(context).pop(true);
                  } else {
                    setState(() {});
                  }
                },
                child: const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.add_circle_outline),
                    SizedBox(width: 8),
                    Text('Create wallet'),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
