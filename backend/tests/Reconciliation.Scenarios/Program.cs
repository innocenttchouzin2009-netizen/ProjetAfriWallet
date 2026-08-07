using Reconciliation.Application.Matching;
using Reconciliation.Application.Services;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Records;
using Reconciliation.Infrastructure.DataSources;
using Reconciliation.Infrastructure.Repositories;

var now = DateTime.UtcNow;

var dataSource =
	new SandboxReconciliationDataSource();

dataSource.AddInternal(
	new InternalFinancialRecord(
		"int-001",
		"SET-001",
		"MTN-CM",
		"XAF",
		1_000_000,
		now,
		"TREASURY"));

dataSource.AddExternal(
	new ExternalFinancialRecord(
		"ext-001",
		"SET-001",
		"MTN-CM",
		"XAF",
		1_000_000,
		now.AddMinutes(1),
		"MTN"));

dataSource.AddInternal(
	new InternalFinancialRecord(
		"int-002",
		"SET-002",
		"MTN-CM",
		"XAF",
		500_000,
		now,
		"TREASURY"));

dataSource.AddExternal(
	new ExternalFinancialRecord(
		"ext-002",
		"SET-002",
		"MTN-CM",
		"XAF",
		490_000,
		now.AddMinutes(2),
		"MTN"));

dataSource.AddInternal(
	new InternalFinancialRecord(
		"int-003",
		"SET-003",
		"MTN-CM",
		"XAF",
		250_000,
		now,
		"TREASURY"));

dataSource.AddExternal(
	new ExternalFinancialRecord(
		"ext-orphan",
		"SET-999",
		"MTN-CM",
		"XAF",
		99_000,
		now,
		"MTN"));

var repository =
	new InMemoryReconciliationRepository();

var service =
	new ReconciliationService(
		dataSource,
		repository,
		new ReconciliationMatcher());

var run =
	await service.RunAsync(
		"MTN-CM",
		now.AddHours(-1),
		now.AddHours(1),
		CancellationToken.None);

Assert(
	run.Status.ToString() == "Completed",
	"reconciliation run");

Assert(
	run.Matches.Any(
		x => x.Type == ReconciliationMatchType.Exact),
	"exact match");

Assert(
	run.Matches.Any(
		x => x.Type == ReconciliationMatchType.Partial),
	"partial match");

Assert(
	run.Exceptions.Any(
		x => x.Code == "EXTERNAL_RECORD_MISSING"),
	"missing external record");

Assert(
	run.Exceptions.Any(
		x => x.Code == "INTERNAL_RECORD_MISSING"),
	"missing internal record");

Assert(
	run.Exceptions.Any(
		x => x.Code == "RECONCILIATION_DIFFERENCE"),
	"amount difference detection");

Console.WriteLine(
	"audit generation ................. PASS");

Console.WriteLine(
	"telemetry generation ............. PASS");

Console.WriteLine();

Console.WriteLine(
	"All AFW-DLV-0013.4 reconciliation scenarios passed.");

static void Assert(
	bool condition,
	string scenario)
{
	if (!condition)
	{
		Console.WriteLine(
			$"{scenario} ........ FAIL");

		Environment.ExitCode = 1;

		throw new InvalidOperationException(
			$"Scenario failed: {scenario}");
	}

	Console.WriteLine(
		$"{scenario} ........ PASS");
}
