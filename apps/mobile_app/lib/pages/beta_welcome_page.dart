import 'package:flutter/material.dart';

import '../theme/afwal_theme.dart';

class BetaWelcomePage extends StatelessWidget {
  const BetaWelcomePage({super.key, required this.onContinue});

  final VoidCallback onContinue;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 24, 24, 28),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: AfWalColors.deepGreen,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    alignment: Alignment.center,
                    child: const Text('AW', style: TextStyle(color: AfWalColors.gold, fontWeight: FontWeight.w800)),
                  ),
                  const SizedBox(width: 12),
                  const Text('AfWal', style: TextStyle(fontSize: 24, fontWeight: FontWeight.w800)),
                ],
              ),
              const Spacer(),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
                decoration: BoxDecoration(
                  color: AfWalColors.gold.withValues(alpha: .16),
                  borderRadius: BorderRadius.circular(999),
                ),
                child: const Text('MOBILE BETA 1', style: TextStyle(fontWeight: FontWeight.w700, letterSpacing: 1.1)),
              ),
              const SizedBox(height: 22),
              const Text(
                'Une identité.\nUn wallet.\nUne Afrique connectée.',
                style: TextStyle(fontSize: 40, height: 1.08, fontWeight: FontWeight.w800, color: AfWalColors.ink),
              ),
              const SizedBox(height: 20),
              Text(
                'Découvrez la première expérience mobile AfWal : simple, sécurisée et pensée pour connecter vos paiements au quotidien.',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(height: 1.5),
              ),
              const Spacer(),
              FilledButton.icon(
                onPressed: onContinue,
                icon: const Icon(Icons.arrow_forward_rounded),
                label: const Text('Découvrir AfWal'),
              ),
              const SizedBox(height: 16),
              const Center(
                child: Text('Connecting Africa. Empowering People.', style: TextStyle(fontWeight: FontWeight.w600)),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
