import 'package:flutter/material.dart';

import '../theme/afwal_theme.dart';

class OnboardingAuthPage extends StatefulWidget {
  const OnboardingAuthPage({super.key, required this.onContinueToBeta});

  final VoidCallback onContinueToBeta;

  @override
  State<OnboardingAuthPage> createState() => _OnboardingAuthPageState();
}

class _OnboardingAuthPageState extends State<OnboardingAuthPage> {
  int _step = 0;

  static const _slides = <({IconData icon, String title, String body})>[
    (icon: Icons.language_rounded, title: 'Une identité pour une Afrique connectée', body: 'Votre expérience AfWal est conçue pour vous accompagner à travers pays, devises et réseaux financiers.'),
    (icon: Icons.qr_code_2_rounded, title: 'Payez et recevez simplement', body: 'Préparez vos paiements, transferts et QR depuis une expérience claire et cohérente.'),
    (icon: Icons.shield_outlined, title: 'La sécurité dès le départ', body: 'PIN, appareils de confiance et AfWal ID sont au cœur de votre identité financière.'),
  ];

  void _next() {
    if (_step < _slides.length - 1) {
      setState(() => _step++);
    } else {
      _openAccess();
    }
  }

  void _openAccess() {
    Navigator.of(context).push(MaterialPageRoute<void>(
      builder: (_) => AccountAccessPage(onContinueToBeta: widget.onContinueToBeta),
    ));
  }

  @override
  Widget build(BuildContext context) {
    final slide = _slides[_step];
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 20, 24, 28),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              Container(width: 44, height: 44, alignment: Alignment.center, decoration: BoxDecoration(color: AfWalColors.deepGreen, borderRadius: BorderRadius.circular(14)), child: const Text('AW', style: TextStyle(color: AfWalColors.gold, fontWeight: FontWeight.w800))),
              const Spacer(),
              TextButton(onPressed: _openAccess, child: const Text('Passer')),
            ]),
            const Spacer(),
            Container(width: 76, height: 76, decoration: BoxDecoration(color: AfWalColors.gold.withValues(alpha: .16), borderRadius: BorderRadius.circular(24)), child: Icon(slide.icon, size: 38, color: AfWalColors.deepGreen)),
            const SizedBox(height: 28),
            Text(slide.title, style: const TextStyle(fontSize: 34, height: 1.08, fontWeight: FontWeight.w800, color: AfWalColors.ink)),
            const SizedBox(height: 18),
            Text(slide.body, style: Theme.of(context).textTheme.bodyLarge?.copyWith(height: 1.5)),
            const Spacer(),
            Row(children: List.generate(_slides.length, (index) => Expanded(child: Container(height: 4, margin: EdgeInsets.only(right: index == _slides.length - 1 ? 0 : 8), decoration: BoxDecoration(color: index <= _step ? AfWalColors.deepGreen : AfWalColors.deepGreen.withValues(alpha: .12), borderRadius: BorderRadius.circular(99))))),),
            const SizedBox(height: 22),
            SizedBox(width: double.infinity, child: FilledButton(onPressed: _next, child: Text(_step == _slides.length - 1 ? 'Commencer' : 'Continuer'))),
          ]),
        ),
      ),
    );
  }
}

class AccountAccessPage extends StatelessWidget {
  const AccountAccessPage({super.key, required this.onContinueToBeta});

  final VoidCallback onContinueToBeta;

  void _showForm(BuildContext context, bool create) {
    Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => AuthFormPage(createAccount: create, onContinueToBeta: onContinueToBeta)));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            IconButton(onPressed: () => Navigator.of(context).pop(), icon: const Icon(Icons.arrow_back_rounded)),
            const Spacer(),
            const Text('Bienvenue sur AfWal', style: TextStyle(fontSize: 36, height: 1.1, fontWeight: FontWeight.w800, color: AfWalColors.ink)),
            const SizedBox(height: 16),
            Text('Créez votre accès ou retrouvez votre compte. Aucune opération financière n’est déclenchée depuis cet écran.', style: Theme.of(context).textTheme.bodyLarge?.copyWith(height: 1.5)),
            const Spacer(),
            SizedBox(width: double.infinity, child: FilledButton(onPressed: () => _showForm(context, true), child: const Text('Créer un compte'))),
            const SizedBox(height: 12),
            SizedBox(width: double.infinity, child: OutlinedButton(onPressed: () => _showForm(context, false), child: const Text('Se connecter'))),
          ]),
        ),
      ),
    );
  }
}

class AuthFormPage extends StatefulWidget {
  const AuthFormPage({super.key, required this.createAccount, required this.onContinueToBeta});
  final bool createAccount;
  final VoidCallback onContinueToBeta;

  @override
  State<AuthFormPage> createState() => _AuthFormPageState();
}

class _AuthFormPageState extends State<AuthFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _contact = TextEditingController();
  String _country = 'Cameroun';

  @override
  void dispose() { _contact.dispose(); super.dispose(); }

  void _submit() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Parcours Beta prêt. La connexion réelle sera activée uniquement via les services backend validés.')));
    widget.onContinueToBeta();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.createAccount ? 'Créer un compte' : 'Se connecter')),
      body: SafeArea(child: Form(key: _formKey, child: ListView(padding: const EdgeInsets.all(24), children: [
        Text(widget.createAccount ? 'Votre accès AfWal' : 'Heureux de vous revoir', style: const TextStyle(fontSize: 30, fontWeight: FontWeight.w800, color: AfWalColors.ink)),
        const SizedBox(height: 24),
        if (widget.createAccount) DropdownButtonFormField<String>(initialValue: _country, decoration: const InputDecoration(labelText: 'Pays'), items: const ['Cameroun', 'Côte d’Ivoire', 'Ghana', 'Nigeria', 'Kenya', 'Allemagne'].map((country) => DropdownMenuItem(value: country, child: Text(country))).toList(), onChanged: (value) => setState(() => _country = value ?? _country)),
        if (widget.createAccount) const SizedBox(height: 16),
        TextFormField(controller: _contact, keyboardType: TextInputType.emailAddress, decoration: const InputDecoration(labelText: 'Téléphone ou e-mail', prefixIcon: Icon(Icons.person_outline_rounded)), validator: (value) => value == null || value.trim().isEmpty ? 'Saisissez votre téléphone ou e-mail.' : null),
        const SizedBox(height: 16),
        TextFormField(obscureText: true, keyboardType: TextInputType.number, decoration: InputDecoration(labelText: widget.createAccount ? 'Créer un PIN' : 'PIN', prefixIcon: const Icon(Icons.lock_outline_rounded), helperText: 'Le PIN sera vérifié par le moteur d’authentification AfWal.'), validator: (value) => value == null || value.length < 6 ? 'Le PIN doit contenir au moins 6 chiffres.' : null),
        const SizedBox(height: 28),
        FilledButton(onPressed: _submit, child: Text(widget.createAccount ? 'Continuer' : 'Se connecter')),
        const SizedBox(height: 12),
        Text('Beta 1.2 • Aucun solde, transaction ou succès financier fictif n’est créé.', textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodySmall),
      ]))),
    );
  }
}
