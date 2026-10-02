import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../models/current_identity_profile.dart';
import '../models/payment_transfer.dart';
import '../services/identity_read_repository.dart';
import '../services/transfer_repository.dart';

class IdentityAwidPage extends StatefulWidget {
  const IdentityAwidPage({
    super.key,
    required this.identityRepository,
    required this.transferRepository,
    required this.onContinue,
  });

  final IdentityReadRepository identityRepository;
  final TransferRepository transferRepository;
  final VoidCallback onContinue;

  @override
  State<IdentityAwidPage> createState() => _IdentityAwidPageState();
}

class _IdentityAwidPageState extends State<IdentityAwidPage> {
  late Future<_IdentityPresentationData> _identityFuture;

  @override
  void initState() {
    super.initState();
    _identityFuture = _loadIdentity();
  }

  Future<_IdentityPresentationData> _loadIdentity() async {
    final profile = await widget.identityRepository.loadCurrentProfile();
    final receiveIdentity =
        await widget.transferRepository.loadReceiveIdentity();

    return _IdentityPresentationData(
      profile: profile,
      receiveIdentity: receiveIdentity,
    );
  }

  void _retry() {
    setState(() => _identityFuture = _loadIdentity());
  }

  Future<void> _copyAfWalId(String afWalId) async {
    await Clipboard.setData(ClipboardData(text: afWalId));
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('AfWal ID copié')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Mon AfWal ID')),
      body: SafeArea(
        child: FutureBuilder<_IdentityPresentationData>(
          future: _identityFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError) {
              return _IdentityUnavailable(
                onRetry: _retry,
                onContinue: widget.onContinue,
              );
            }

            final identity = snapshot.data;
            if (identity == null) {
              return _IdentityUnavailable(
                onRetry: _retry,
                onContinue: widget.onContinue,
              );
            }

            return ListView(
              padding: const EdgeInsets.all(24),
              children: [
                Text(
                  'Votre identité financière',
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  'Votre AfWal ID est votre identifiant public. Il ne donne jamais accès à vos fonds ni à vos identifiants de connexion.',
                  style: Theme.of(context).textTheme.bodyLarge,
                ),
                const SizedBox(height: 24),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'AFWAL ID',
                          style: TextStyle(fontWeight: FontWeight.w700),
                        ),
                        const SizedBox(height: 14),
                        Text(
                          identity.receiveIdentity.publicLabel,
                          style: Theme.of(context).textTheme.headlineSmall,
                        ),
                        const SizedBox(height: 12),
                        const Text(
                          'Identité de connexion',
                          style: TextStyle(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(height: 4),
                        Text(identity.profile.identifier),
                        const SizedBox(height: 16),
                        const Row(
                          children: [
                            Icon(Icons.verified_user_outlined, size: 18),
                            SizedBox(width: 8),
                            Text('Identité vérifiée par le backend'),
                          ],
                        ),
                        const SizedBox(height: 16),
                        OutlinedButton.icon(
                          onPressed: () => _copyAfWalId(
                            identity.receiveIdentity.publicLabel,
                          ),
                          icon: const Icon(Icons.copy_rounded),
                          label: const Text('Copier mon AfWal ID'),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(20),
                    child: Column(
                      children: [
                        const Icon(Icons.qr_code_2_rounded, size: 72),
                        const SizedBox(height: 12),
                        const Text('QR AfWal ID'),
                        const SizedBox(height: 8),
                        Text(
                          identity.receiveIdentity.hasBackendQr
                              ? 'Un jeton QR sécurisé est disponible depuis le backend.'
                              : 'Le QR dynamique sera affiché uniquement lorsqu’un jeton QR valide sera fourni par le backend.',
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: widget.onContinue,
                  child: const Text('Continuer'),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _IdentityPresentationData {
  const _IdentityPresentationData({
    required this.profile,
    required this.receiveIdentity,
  });

  final CurrentIdentityProfile profile;
  final ReceiveIdentity receiveIdentity;
}

class _IdentityUnavailable extends StatelessWidget {
  const _IdentityUnavailable({
    required this.onRetry,
    required this.onContinue,
  });

  final VoidCallback onRetry;
  final VoidCallback onContinue;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.badge_outlined, size: 64),
          const SizedBox(height: 16),
          Text(
            'AfWal ID indisponible',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          const Text(
            'Aucune identité n’est simulée. Les données doivent provenir des services backend autoritatifs.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 20),
          FilledButton.tonal(
            onPressed: onRetry,
            child: const Text('Réessayer'),
          ),
          TextButton(
            onPressed: onContinue,
            child: const Text('Continuer sans afficher mon ID'),
          ),
        ],
      ),
    );
  }
}
