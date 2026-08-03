import 'package:flutter/material.dart';

import '../core/theme/afri_theme.dart';
import '../features/awid/presentation/africard_screen.dart';

class AfriWalletApp extends StatelessWidget {
  const AfriWalletApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'AfriWallet',
      theme: AfriTheme.light(),
      home: const AfriCardScreen(),
    );
  }
}
