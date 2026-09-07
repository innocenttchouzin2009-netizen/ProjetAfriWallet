import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_receipt_verification.dart';
import '../services/subscription_receipt_verification_codec.dart';

typedef ReceiptVerificationNowProvider = DateTime Function();

class SubscriptionReceiptVerificationPage extends StatelessWidget {
  const SubscriptionReceiptVerificationPage({
    super.key,
    required this.rawCode,
    this.codec = const SubscriptionReceiptVerificationCodec(),
    this.nowProvider = _defaultNow,
  });

  final String rawCode;
  final SubscriptionReceiptVerificationCodec codec;
  final ReceiptVerificationNowProvider nowProvider;

  static DateTime _defaultNow() => DateTime.now().toUtc();

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;

    SubscriptionReceiptVerificationPayload? payload;
    var state = _ReceiptVerificationViewState.invalid;

    try {
      payload = codec.decode(rawCode);
      state = payload.isExpiredAt(nowProvider())
          ? _ReceiptVerificationViewState.expired
          : _ReceiptVerificationViewState.valid;
    } on InvalidSubscriptionReceiptVerificationException {
      state = _ReceiptVerificationViewState.invalid;
    }

    final title = switch (state) {
      _ReceiptVerificationViewState.valid =>
        localizations.receiptVerificationValid,
      _ReceiptVerificationViewState.invalid =>
        localizations.receiptVerificationInvalid,
      _ReceiptVerificationViewState.expired =>
        localizations.receiptVerificationExpired,
    };
    final message = switch (state) {
      _ReceiptVerificationViewState.valid =>
        localizations.receiptVerificationValidMessage,
      _ReceiptVerificationViewState.invalid =>
        localizations.receiptVerificationInvalidMessage,
      _ReceiptVerificationViewState.expired =>
        localizations.receiptVerificationExpiredMessage,
    };
    final icon = switch (state) {
      _ReceiptVerificationViewState.valid => Icons.verified_outlined,
      _ReceiptVerificationViewState.invalid => Icons.error_outline,
      _ReceiptVerificationViewState.expired => Icons.schedule_outlined,
    };

    return Scaffold(
      appBar: AppBar(title: Text(localizations.receiptVerification)),
      body: SafeArea(
        child: ListView(
          key: const Key('subscription-receipt-verification-page'),
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              key: Key('receipt-verification-${state.name}'),
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    ExcludeSemantics(child: Icon(icon, size: 56)),
                    const SizedBox(height: 16),
                    Semantics(
                      liveRegion: true,
                      label: title,
                      child: ExcludeSemantics(
                        child: Text(
                          title,
                          textAlign: TextAlign.center,
                          style: Theme.of(context).textTheme.headlineSmall,
                        ),
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(message, textAlign: TextAlign.center),
                    if (payload != null) ...[
                      const SizedBox(height: 24),
                      _VerificationRow(
                        label: localizations.invoiceId,
                        value: payload.invoiceId,
                      ),
                      const Divider(),
                      _VerificationRow(
                        label: localizations.paymentReference,
                        value: payload.paymentReference,
                      ),
                    ],
                    const SizedBox(height: 20),
                    Text(
                      localizations.receiptVerificationHint,
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 20),
                    FilledButton(
                      key: const Key('receipt-verification-done'),
                      onPressed: () => Navigator.of(context).pop(),
                      child: Text(localizations.done),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

enum _ReceiptVerificationViewState { valid, invalid, expired }

class _VerificationRow extends StatelessWidget {
  const _VerificationRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return MergeSemantics(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: Theme.of(context).textTheme.labelMedium),
            const SizedBox(height: 4),
            Text(value, style: Theme.of(context).textTheme.bodyLarge),
          ],
        ),
      ),
    );
  }
}
