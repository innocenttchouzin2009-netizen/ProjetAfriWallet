import 'package:flutter/material.dart';
import 'features/fx/fx_quote_screen.dart';
import 'features/wallets/wallet_router.dart';
import 'features/wallets/wallet_screen.dart';
import 'l10n/app_localizations.dart';

void main() {
  runApp(const AfriWalletApp());
}

class AfriWalletApp extends StatefulWidget {
  const AfriWalletApp({super.key});

  @override
  State<AfriWalletApp> createState() => _AfriWalletAppState();
}

class _AfriWalletAppState extends State<AfriWalletApp> {
  ThemeMode _themeMode = ThemeMode.system;
  Locale _locale = const Locale('en');

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AfriWallet',
      theme: ThemeData(colorSchemeSeed: Colors.indigo, useMaterial3: true),
      darkTheme: ThemeData(colorSchemeSeed: Colors.indigo, brightness: Brightness.dark, useMaterial3: true),
      themeMode: _themeMode,
      locale: _locale,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      supportedLocales: AppLocalizations.supportedLocales,
      home: const WalletScreen(),
      onGenerateRoute: WalletRouter.onGenerateRoute,
      builder: (context, child) {
        return Scaffold(
          body: child,
          bottomNavigationBar: NavigationBar(
            destinations: [
              const NavigationDestination(icon: Icon(Icons.account_balance_wallet), label: 'Wallets'),
              NavigationDestination(icon: const Icon(Icons.currency_exchange), label: 'FX'),
            ],
            selectedIndex: 0,
            onDestinationSelected: (index) {
              if (index == 1) {
                Navigator.of(context).push(MaterialPageRoute(builder: (_) => const FxQuoteScreen()));
              } else {
                Navigator.of(context).pushReplacementNamed('/wallets');
              }
            },
          ),
        );
      },
    );
  }
}
