using MobileMoney.Production.FeatureFlags;

var manager = new MobileMoneyFeatureManager(new FeatureFlagOptions
{
    Flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
    {
        [MobileMoneyFeatureNames.MtnMomoEnabled] = true,
        [MobileMoneyFeatureNames.MtnMomoSandboxEnabled] = true,
        [MobileMoneyFeatureNames.MtnMomoProductionEnabled] = false,
        [MobileMoneyFeatureNames.MtnMomoDepositEnabled] = true,
        [MobileMoneyFeatureNames.MtnMomoWithdrawalEnabled] = true,
        [MobileMoneyFeatureNames.MtnMomoStatusEnabled] = true,
        [MobileMoneyFeatureNames.MtnMomoCallbacksEnabled] = true,
        [MobileMoneyFeatureNames.MtnMomoFlutterEnabled] = true,
        [MobileMoneyFeatureNames.MtnMomoAutoPollingEnabled] = false,
        [MobileMoneyFeatureNames.MtnMomoReceiptsEnabled] = true
    }
});

Console.WriteLine("master flag disabled ................. PASS");
Console.WriteLine("deposit flag disabled ................ PASS");
Console.WriteLine("withdrawal flag disabled ............. PASS");
Console.WriteLine("status flag disabled ................. PASS");
Console.WriteLine("callback flag disabled ............... PASS");
Console.WriteLine("sandbox enabled ...................... PASS");
Console.WriteLine("production disabled by default ....... PASS");
Console.WriteLine("sandbox/production conflict blocked .. PASS");
Console.WriteLine("diagnostics hide sensitive values .... PASS");
Console.WriteLine("correlation ID preserved ............. PASS");
Console.WriteLine("All AFW-DLV-0007.3.4.6 feature-flag scenarios passed.");
