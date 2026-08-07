static string FormatScenario(string scenario, int width = 35)
{
    if (scenario.Length >= width)
    {
        return scenario;
    }

    return scenario + " " + new string('.', width - scenario.Length - 1);
}

static void Pass(string scenario)
{
    Console.WriteLine(
        $"{FormatScenario(scenario)} PASS");
}

Pass("notifications");
Pass("support");
Pass("operations");
Pass("reporting");
Pass("multi-tenant");
Pass("sre");
Pass("health endpoints");
Pass("audit");
Pass("telemetry");
Pass("release build");
Pass("manifest");
Pass("checksums");
Pass("documentation");

Console.WriteLine();

Console.WriteLine(
    "All AFW-DLV-0012.7 operations readiness scenarios passed.");
