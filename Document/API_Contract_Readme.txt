# API Contract

Bu dokuman PKG-011 + PKG-013 kapsaminda aktif endpointleri ozetler.

## Ortak Notlar
- Auth: Bearer JWT
- Group allowlist policy: zorunlu
- DB allowlist policy: `AppUsers` tablosunda kayitli + `IsActive=true` degilse 403 (fail-closed)
- Correlation header: `X-Correlation-Id` (opsiyonel, yoksa API uretir)

## Auth
- `POST /auth/login`
  - Request: `LoginRequest`
  - Response: `LoginResponse`
  - Not: OIDC authorize URL dondurur

## Jobs
- `POST /jobs/discovery`
  - Role: `Admin,Operator`
  - Request: `CreateDiscoveryJobRequest`
  - Response: `JobCreatedResponse`

- `POST /jobs/password-change`
  - Role: `Admin,Operator`
  - Request: `CreatePasswordChangeJobRequest`
  - Response: `JobCreatedResponse`
  - Not: `oldPassword` ve `newPassword` ayni olamaz
  - Akis: Worker AD (LDAPS) change -> update -> (opsiyonel) verify

- `GET /jobs/{id}`
  - Role: `Admin,Operator,Viewer`
  - Response: `JobStatusResponse`
  - Not: Job erisim kontrolu uygulanir (Admin/SuperAdmin tum job'lar; diger rollerde sadece kendi job'u)

- `GET /jobs/{id}/targets?skip=0&take=200`
  - Role: `Admin,Operator,Viewer`
  - Response: `JobTargetsResponse`
  - Not: `skip >= 0`, `take` araligi `1-2000`

## Audit
- `GET /audit/logs?take=100&correlationId=<id>`
  - Role: `Admin,SuperAdmin`
  - Response: `AuditLogResponse`
  - Not: max `take=500`, correlationId ile filtrelenebilir

## Admin (User/Role)
- `GET /admin/users?skip=0&take=100`
  - Role: `Admin,SuperAdmin`
  - Response: `ListUsersResponse`
  - Not: `skip >= 0`, `take` araligi `1-500`

- `POST /admin/users`
  - Role: `Admin,SuperAdmin`
  - Request: `UpsertUserRequest`
  - Response: `UpsertUserResponse`

- `PUT /admin/users/{id}/active`
  - Role: `Admin,SuperAdmin`
  - Request: `SetUserActiveRequest`
  - Response: `204 NoContent`
  - Not: Son aktif admin/superadmin pasif edilemez (409 Conflict)

- `PUT /admin/users/{id}/roles`
  - Role: `Admin,SuperAdmin`
  - Request: `SetUserRolesRequest`
  - Response: `204 NoContent`
  - Not: `SuperAdmin` rolunu atamak icin cagirani `SuperAdmin` olmalidir (aksi halde 403)
  - Not: Son aktif admin/superadmin'in Admin/SuperAdmin rolleri kaldirilamaz (409 Conflict)

- `GET /admin/roles`
  - Role: `Admin,SuperAdmin`
  - Response: `RoleListResponse`
