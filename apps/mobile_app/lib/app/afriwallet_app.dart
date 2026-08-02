import 'package:afw_design_system/afw_design_system.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import '../core/router/app_router.dart';
import '../l10n/generated/app_localizations.dart';

class AfriWalletApp extends StatelessWidget {
  const AfriWalletApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'AfriWallet',
      debugShowCheckedModeBanner: false,
      theme: AfwTheme.light(),
      darkTheme: AfwTheme.dark(),
      themeMode: ThemeMode.system,
      routerConfig: appRouter,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ],
    );
  }
}
