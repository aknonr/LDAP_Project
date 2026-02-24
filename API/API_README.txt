# API Katmani

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Amac
- OIDC login, RBAC kontrolu, job endpointleri, audit raporlama ve SignalR canli guncelleme bu katmanda calisir.

## Tamamlanan Paketler
- `PKG-011 Adim 3`: Controller + DI wiring + MQ publish
- `PKG-012`: SignalR hub ve result event consumer koprusu
- `PKG-013`: Correlation middleware, request logging enrich, security headers, audit endpoint
- `PKG-018`: Authorization hardening: DB allowlist + job/hub authz
- `PKG-019 (kismi)`: Admin user/role API (soft delete: IsActive) + audit

## Uygulanan Bilesenler
- `Controllers/AuthController.cs`
- `Controllers/JobsController.cs`
- `Controllers/AuditController.cs`
- `Controllers/AdminUsersController.cs`
- `Controllers/AdminRolesController.cs`
- `Hubs/JobsHub.cs`
- `Consumers/ServerUsageResultEventConsumer.cs`
- `Consumers/ServerUpdateResultEventConsumer.cs`
- `Consumers/JobProgressEventConsumer.cs`
- `Logging/CorrelationIdMiddleware.cs`
- `Logging/SecurityHeadersMiddleware.cs`
- `Startup/RbacBootstrapHostedService.cs` (role seed + opsiyonel bootstrap admin)

## API Guvenlik Standartlari
- Tum endpointler default/fallback policy ile authenticated + group allowlist + DB allowlist zorunlu (fail-closed).
- Job baslatma endpointleri sadece `Admin,Operator`.
- Audit rapor endpointi `Admin,SuperAdmin`.
- Admin user/role endpointleri `Admin,SuperAdmin`.
- `X-Correlation-Id` header normalize edilip log ve response'a yazilir.
- Response security headers zorunlu eklenir (`nosniff`, `deny frame`, CSP).
- Password alanlari loglanmaz; audit summary sanitize edilir.
- UI ayri deploy olacaksa CORS config kullanilir: `Cors:AllowedOrigins`.
- SignalR JWT destegi: browser client icin hub path'te `access_token` querystring okunur.

## Akis Notu
- `POST /jobs/password-change` job-level orkestrasyon komutunu publish eder.
- Worker tarafinda akis: AD (LDAPS) change (old+new) -> update -> (opsiyonel) verify.

- RBAC notu: Kullanici DB'de yoksa 403. Ilk kurulumda `Auth:Bootstrap` veya manuel SQL ile en az 1 admin olusturulmalidir.

## SignalR Kurulum Notu (Windows)
- Ayrica "SignalR server" kurulumu yapman gerekmez; ASP.NET Core icinde gelir.
- Gereken temel kurulum:
  - `.NET 8 SDK` (dev)
  - IIS ile host edilecekse `.NET 8 Hosting Bundle`
  - IIS Windows Feature: `WebSocket Protocol` acik olmali
- Bu projede hub yolu: `/hubs/jobs`
- Event adlari: `jobUpdated`, `targetUpdated`

## Paket Durumu
- `Swashbuckle.AspNetCore` `10.1.0`
- `MassTransit` `8.4.1`
- `MassTransit.RabbitMQ` `8.4.1`
- `MassTransit.EntityFrameworkCore` `8.4.1` (EF outbox/inbox)
- `Microsoft.AspNetCore.Authentication.JwtBearer` `8.0.23`
- SignalR server icin ek NuGet zorunlu degil (`Microsoft.AspNetCore.App` ile gelir).
- Scale-out opsiyonu (ileri asama): `Microsoft.AspNetCore.SignalR.StackExchangeRedis`

## Sonraki Plan
- DTO ayirma katmani kullanici istegine gore en sona birakildi.
- PKG-014 UI asamasinda SignalR client abonelikleri ve paging/virtualization baglanacak.
