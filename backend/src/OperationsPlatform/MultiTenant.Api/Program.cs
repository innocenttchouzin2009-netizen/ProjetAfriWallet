using MultiTenant.Api;
using MultiTenant.Application.Interfaces;
using MultiTenant.Application.Security;
using MultiTenant.Application.Services;
using MultiTenant.Contracts.Requests;
using MultiTenant.Contracts.Responses;
using MultiTenant.Domain.Memberships;
using MultiTenant.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
builder.Services.AddSingleton<ITenantMembershipRepository, InMemoryTenantMembershipRepository>();
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddScoped<TenantAdministrationService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new
{
	status = "Healthy",
	service = "afriwallet-multi-tenant-platform"
}));

app.UseMiddleware<TenantResolutionMiddleware>();

app.MapPost(
	"/api/v1/tenants",
	async (
		CreateTenantRequest request,
		TenantAdministrationService service,
		CancellationToken cancellationToken) =>
	{
		var tenant = await service.CreateTenantAsync(
			request.TenantCode,
			request.LegalName,
			request.DisplayName,
			request.CountryCode,
			request.BaseCurrency,
			request.AdministratorSubjectId,
			cancellationToken);

		return Results.Created(
			$"/api/v1/tenants/{tenant.TenantId}",
			TenantResponse.From(tenant));
	});

app.MapGet(
	"/api/v1/tenants/{tenantId:guid}",
	async (
		Guid tenantId,
		ITenantContextAccessor contextAccessor,
		TenantAdministrationService service,
		CancellationToken cancellationToken) =>
	{
		var context = contextAccessor.Current
			?? throw new InvalidOperationException("Tenant context is unavailable.");

		await service.EnsureTenantAccessAsync(
			context.TenantId,
			tenantId,
			cancellationToken);

		var tenant = await service.GetTenantAsync(
			tenantId,
			cancellationToken);

		return tenant is null
			? Results.NotFound(new
			{
				code = "TENANT_NOT_FOUND",
				correlationId = contextAccessor.Current?.SubjectId
			})
			: Results.Ok(TenantResponse.From(tenant));
	});

app.MapPost(
	"/api/v1/tenants/{tenantId:guid}/activate",
	async (
		Guid tenantId,
		ITenantContextAccessor accessor,
		TenantAdministrationService service,
		CancellationToken cancellationToken) =>
	{
		var context = RequirePermission(accessor, TenantPermissions.TenantWrite);

		await service.EnsureTenantAccessAsync(
			context.TenantId,
			tenantId,
			cancellationToken);

		var tenant = await service.GetTenantAsync(
			tenantId,
			cancellationToken);

		if (tenant is null)
		{
			return Results.NotFound();
		}

		tenant.Activate();

		return Results.Ok(TenantResponse.From(tenant));
	});

app.MapPost(
	"/api/v1/tenants/{tenantId:guid}/members",
	async (
		Guid tenantId,
		AddTenantMemberRequest request,
		ITenantContextAccessor accessor,
		TenantAdministrationService service,
		CancellationToken cancellationToken) =>
	{
		var context = RequirePermission(accessor, TenantPermissions.MemberWrite);

		await service.EnsureTenantAccessAsync(
			context.TenantId,
			tenantId,
			cancellationToken);

		var membership = await service.AddMemberAsync(
			tenantId,
			request.SubjectId,
			request.Roles,
			request.Permissions,
			cancellationToken);

		return Results.Created(
			$"/api/v1/tenants/{tenantId}/members/{membership.SubjectId}",
			new
			{
				membership.MembershipId,
				membership.TenantId,
				membership.SubjectId,
				membership.Roles,
				membership.Permissions,
				status = membership.Status.ToString()
			});
	});

app.MapOpenApi();

app.Run();

static TenantContext RequirePermission(
	ITenantContextAccessor accessor,
	string permission)
{
	var context = accessor.Current
		?? throw new InvalidOperationException("Tenant context is unavailable.");

	if (!context.HasPermission(permission))
	{
		throw new UnauthorizedAccessException($"Permission '{permission}' is required.");
	}

	return context;
}

public partial class Program;
