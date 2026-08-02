import 'package:afw_design_system/afw_design_system.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_router.dart';
import '../../../../l10n/generated/app_localizations.dart';

class WelcomeScreen extends StatelessWidget {
  const WelcomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = AppLocalizations.of(context);

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AfwSpacing.x3),
          child: Column(
            children: [
              Align(
                alignment: AlignmentDirectional.centerEnd,
                child: TextButton.icon(
                  onPressed: () {},
                  icon: const Icon(Icons.language_rounded),
                  label: Text(l10n.chooseLanguage),
                ),
              ),
              const Spacer(),
              Image.asset(
                'assets/branding/afriwallet_logo.png',
                width: 210,
                errorBuilder:
                    (_, _, _) => const Icon(
                      Icons.account_balance_wallet_rounded,
                      size: 112,
                    ),
              ),
              const SizedBox(height: AfwSpacing.x4),
              Text(
                l10n.welcomeTitle,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: AfwSpacing.x2),
              Text(
                l10n.welcomeSubtitle,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyLarge,
              ),
              const Spacer(),
              AfwPrimaryButton(
                label: l10n.createAccount,
                icon: Icons.arrow_forward_rounded,
                onPressed: () => context.go(AppRoutes.financialDesk),
              ),
              const SizedBox(height: AfwSpacing.x2),
              TextButton(
                onPressed: () => context.go(AppRoutes.financialDesk),
                child: Text(l10n.signIn),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
