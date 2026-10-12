var scenarios = new (string Name, Func<Task> Run)[]
{
    ("execution binding survives store recreation", DurabilityScenarios.PersistsBindingAcrossStoreRecreation),
    ("execution binding same-instance save is idempotent", DurabilityScenarios.RepeatedSaveOfSameBindingIsIdempotent),
    ("execution binding rejects conflicting idempotency key", DurabilityScenarios.RejectsConflictingIdempotencyKey)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine(
    $"Mobile Money payout execution durability scenarios: {scenarios.Length}/{scenarios.Length} passed.");
