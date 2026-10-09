import 'package:flutter/foundation.dart';

import '../models/mobile_money_payout.dart';
import '../models/mobile_money_payout_funding_intent.dart';
import '../models/mobile_money_payout_quote.dart';
import '../services/mobile_money_payout_funding_allocation_planner.dart';

enum MobileMoneyPayoutFundingSelectionMode {
  walletOnly,
  externalOnly,
  split,
}

class MobileMoneyPayoutFundingSelectionController extends ChangeNotifier {
  MobileMoneyPayoutFundingSelectionController({
    required this.quote,
    required Iterable<FundingSource> fundingSources,
  }) : _fundingSources = List<FundingSource>.unmodifiable(fundingSources) {
    _validateFundingSources();
  }

  final MobileMoneyPayoutQuote quote;
  final List<FundingSource> _fundingSources;

  MobileMoneyPayoutFundingSelectionMode? _selectedMode;
  MobileMoneyPayoutFundingIntent? _selectedIntent;
  String? _selectedExternalSourceId;

  List<FundingSource> get fundingSources => _fundingSources;
  MobileMoneyPayoutFundingSelectionMode? get selectedMode => _selectedMode;
  MobileMoneyPayoutFundingIntent? get selectedIntent => _selectedIntent;
  String? get selectedExternalSourceId => _selectedExternalSourceId;

  FundingSource? get walletSource {
    for (final source in _fundingSources) {
      if (source.type == FundingSourceType.wallet &&
          source.isAvailable &&
          source.currencyCode == quote.sourceCurrencyCode) {
        return source;
      }
    }
    return null;
  }

  List<FundingSource> get availableExternalSources =>
      List<FundingSource>.unmodifiable(
        _fundingSources.where(
          (source) =>
              source.type != FundingSourceType.wallet &&
              source.isAvailable &&
              source.currencyCode == quote.sourceCurrencyCode,
        ),
      );

  bool get canUseWalletOnly {
    final wallet = walletSource;
    return wallet != null &&
        wallet.availableMinor! >= quote.totalSourceDebitMinor;
  }

  bool get canUseSplit {
    final wallet = walletSource;
    return wallet != null &&
        wallet.availableMinor! > 0 &&
        wallet.availableMinor! < quote.totalSourceDebitMinor &&
        availableExternalSources.isNotEmpty;
  }

  int? get splitWalletAmountMinor =>
      canUseSplit ? walletSource!.availableMinor : null;

  int? get splitExternalAmountMinor => canUseSplit
      ? quote.totalSourceDebitMinor - walletSource!.availableMinor!
      : null;

  void selectWalletOnly() {
    final wallet = walletSource;
    if (!canUseWalletOnly || wallet == null) {
      throw StateError(
        'Wallet-only funding is not available for this quote.',
      );
    }

    final allocations =
        MobileMoneyPayoutFundingAllocationPlanner.planWalletOnly(
      quote: quote,
      walletSource: wallet,
    );

    _setSelection(
      mode: MobileMoneyPayoutFundingSelectionMode.walletOnly,
      allocations: allocations,
    );
  }

  void selectExternal(FundingSource externalSource) {
    final source = _requireAvailableExternalSource(externalSource.id);
    final allocations =
        MobileMoneyPayoutFundingAllocationPlanner.planExternalOnly(
      quote: quote,
      externalSource: source,
    );

    _setSelection(
      mode: MobileMoneyPayoutFundingSelectionMode.externalOnly,
      allocations: allocations,
      externalSourceId: source.id,
    );
  }

  void selectSplit(FundingSource externalSource) {
    final wallet = walletSource;
    if (!canUseSplit || wallet == null) {
      throw StateError(
        'Split funding is not available for this quote.',
      );
    }

    final source = _requireAvailableExternalSource(externalSource.id);
    final allocations = MobileMoneyPayoutFundingAllocationPlanner.planSplit(
      quote: quote,
      walletSource: wallet,
      externalSource: source,
      walletAmountMinor: wallet.availableMinor!,
    );

    _setSelection(
      mode: MobileMoneyPayoutFundingSelectionMode.split,
      allocations: allocations,
      externalSourceId: source.id,
    );
  }

  void clearSelection() {
    if (_selectedMode == null &&
        _selectedIntent == null &&
        _selectedExternalSourceId == null) {
      return;
    }

    _selectedMode = null;
    _selectedIntent = null;
    _selectedExternalSourceId = null;
    notifyListeners();
  }

  FundingSource _requireAvailableExternalSource(String sourceId) {
    for (final source in availableExternalSources) {
      if (source.id == sourceId) {
        return source;
      }
    }

    throw StateError(
      'The selected external funding source is not available.',
    );
  }

  void _setSelection({
    required MobileMoneyPayoutFundingSelectionMode mode,
    required List<FundingAllocation> allocations,
    String? externalSourceId,
  }) {
    final intent = MobileMoneyPayoutFundingIntent(
      quoteId: quote.quoteId,
      sourceCurrencyCode: quote.sourceCurrencyCode,
      totalSourceDebitMinor: quote.totalSourceDebitMinor,
      fundingAllocations: allocations,
    );
    intent.validate(fundingSources: _fundingSources);

    _selectedMode = mode;
    _selectedIntent = intent;
    _selectedExternalSourceId = externalSourceId;
    notifyListeners();
  }

  void _validateFundingSources() {
    final sourceIds = <String>{};
    var walletCount = 0;

    for (final source in _fundingSources) {
      source.validate();

      if (!sourceIds.add(source.id)) {
        throw ArgumentError.value(
          source.id,
          'fundingSources.id',
          'must be unique',
        );
      }

      if (source.type == FundingSourceType.wallet) {
        walletCount += 1;
      }
    }

    if (walletCount > 1) {
      throw ArgumentError.value(
        walletCount,
        'fundingSources',
        'must contain at most one wallet source',
      );
    }
  }
}
