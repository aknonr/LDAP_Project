# API Contract

Bu dokuman PKG-011 + PKG-013 kapsaminda aktif endpointleri ozetler.

## Ortak Notlar
- Auth: Bearer JWT
- Group allowlist policy: zorunlu
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

- `GET /jobs/{id}`
  - Role: `Admin,Operator,Viewer`
  - Response: `JobStatusResponse`

- `GET /jobs/{id}/targets?skip=0&take=200`
  - Role: `Admin,Operator,Viewer`
  - Response: `JobTargetsResponse`
  - Not: `take` araligi `1-2000`

## Audit
- `GET /audit/logs?take=100&correlationId=<id>`
  - Role: `Admin,SuperAdmin`
  - Response: `AuditLogResponse`
  - Not: max `take=500`, correlationId ile filtrelenebilir
