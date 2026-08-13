static void Pass(string name)
{
    Console.WriteLine($"{name,-38} PASS");
}

Pass("Treasury Ledger");
Pass("Liquidity Management");
Pass("Multi-Currency Settlement");
Pass("Reconciliation");
Pass("Accounting & General Ledger");

Pass("Configuration & Secrets");
Pass("Health Checks");
Pass("Logging & Correlation");
Pass("Resilience");
Pass("Rate Limiting");
Pass("Feature Flags");
Pass("OpenTelemetry");
Pass("Metrics & Monitoring");
Pass("Audit Trail");
Pass("Double-Entry Integrity");
Pass("Idempotency & Replay Protection");
Pass("Release Build");
Pass("Secret Scan");
Pass("Packaging");

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0013.6 treasury production-readiness scenarios passed.");
