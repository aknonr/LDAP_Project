# Audit ve Logging (PKG-013)

## Amaç
- Islem izlenebilirligi, guvenlik denetimi ve operasyonel troubleshooting.

## Uygulanan Bilesenler
- Correlation middleware:
  - `X-Correlation-Id` normalize edilir.
  - Request/response uzerinde tasinir.
  - Serilog context'e eklenir.
- Request logging:
  - Host, scheme, user, correlation enriched.
- Audit yazma/okuma:
  - `IAuditTrailWriter`
  - `IAuditTrailReader`
  - `Infrastructure/Persistence/Auditing/AuditTrailStore.cs`
- Audit endpoint:
  - `GET /audit/logs`
  - Role: `Admin,SuperAdmin`

## Audit Alanlari
- `who`
- `when`
- `ticketRef`
- `targetAccount`
- `serverGroup`
- `resultSummary`
- `correlationId`

## Redaction Politikasi
- `password`, `oldPassword`, `newPassword`, `token`, `secret`, `authorization` benzeri desenler redacted yazilir.
- Password degerleri loglanmaz.
- MQ payload sifresi AES-GCM ile sifrelidir.

## Paketler
- `Serilog.AspNetCore`
- `Serilog.Sinks.File`
- `Serilog.Sinks.EventLog`
