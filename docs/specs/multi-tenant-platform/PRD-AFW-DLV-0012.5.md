# AFW-DLV-0012.5 Multi-Tenant Administration Platform

This delivery introduces tenant-scoped administration for AfriWallet.

Every business read and write must carry a `TenantId` and enforce tenant isolation server-side before repository access.