# ADR-0205 Tenant Data Isolation

Cross-tenant access is denied by default. Commands and queries must resolve a tenant context before touching any repository.