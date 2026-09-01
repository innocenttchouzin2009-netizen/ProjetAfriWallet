import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../models/awid_profile.dart';
import '../services/identity_repository.dart';

class IdentityAwidPage extends StatefulWidget {
  const IdentityAwidPage({
    super.key,
    required this.repository,
    required this.onContinue,
  });

  final IdentityRepository repository;
  final VoidCallback onContinue;

  @override
  State<IdentityAwidPage> createState() => _IdentityAwidPageState();
}

class _IdentityAwidPageState extends State<IdentityAwidPage> {
  late Future<AwidProfile> _profileFuture;

  @override
  void initState() {
    super.initState();
    _profileFuture = widget.repository.loadCurrentProfile();
  }

  void _retry() {
    setState(() => _profileFuture = widget.repository.loadCurrentProfile());
  }

  Future<void> _copyAwid(String awid) async {
    await Clipboard.setData(ClipboardData(text: awid));
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
        child: FutureBuilder<AwidProfile>(
          future: _profileFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError) {
              return _IdentityUnavailable(onRetry: _retry, onContinue: widget.onContinue);
            }

            final profile = snapshot.data;
            if (profile == null) {
              return _IdentityUnavailable(onRetry: _retry, onContinue: widget.onContinue);
            }

            return ListView(
              padding: const EdgeInsets.all(24),
              children: [
                Text('Votre identité financière', style: Theme.of(context).textTheme.headlineMedium),
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
                        const Text('AFWAL ID', style: TextStyle(fontWeight: FontWeight.w700)),
                        const SizedBox(height: 14),
                        Text(profile.publicLabel, style: Theme.of(context).textTheme.headlineSmall),
                        const SizedBox(height: 6),
                        Text(profile.displayName),
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Icon(profile.isPrivate ? Icons.lock_outline : Icons.public, size: 18),
                            const SizedBox(width: 8),
                            Text(profile.isPrivate ? 'Profil privé' : 'Profil public'),
                          ],
                        ),
                        const SizedBox(height: 16),
                        OutlinedButton.icon(
                          onPressed: () => _copyAwid(profile.publicLabel),
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
                        const Text(
                          'Le QR dynamique sera affiché uniquement lorsqu’un jeton QR valide sera fourni par le backend.',
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 24),
                FilledButton(onPressed: widget.onContinue, child: const Text('Continuer')),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _IdentityUnavailable extends StatelessWidget {
  const _IdentityUnavailable({required this.onRetry, required this.onContinue});

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
          Text('AfWal ID indisponible', style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 8),
          const Text(
            'Aucune identité n’est simulée. Connectez le service d’identité pour afficher votre véritable AfWal ID.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 20),
          FilledButton.tonal(onPressed: onRetry, child: const Text('Réessayer')),
          TextButton(onPressed: onContinue, child: const Text('Continuer sans afficher mon ID')),
        ],
      ),
    );
  }
}
