# API Katmani

## Amac
- OIDC login, RBAC kontrolu, job endpointleri, audit raporlama ve SignalR canli guncelleme bu katmanda calisir.

## Tamamlanan Paketler
- `PKG-011 Adim 3`: Controller + DI wiring + MQ publish
- `PKG-012`: SignalR hub ve result event consumer koprusu
- `PKG-013`: Correlation middleware, request logging enrich, security headers, audit endpoint

## Uygulanan Bilesenler
- `Controllers/AuthController.cs`
- `Controllers/JobsController.cs`
- `Controllers/AuditController.cs`
- `Hubs/JobsHub.cs`
- `Consumers/ServerUsageResultEventConsumer.cs`
- `Consumers/ServerUpdateResultEventConsumer.cs`
- `Consumers/JobProgressEventConsumer.cs`
- `Logging/CorrelationIdMiddleware.cs`
- `Logging/SecurityHeadersMiddleware.cs`

## API Guvenlik Standartlari
- Tum endpointler fallback policy ile authenticated + group allowlist zorunlu.
- Job baslatma endpointleri sadece `Admin,Operator`.
- Audit rapor endpointi `Admin,SuperAdmin`.
- `X-Correlation-Id` header normalize edilip log ve response'a yazilir.
- Response security headers zorunlu eklenir (`nosniff`, `deny frame`, CSP).
- Password alanlari loglanmaz; audit summary sanitize edilir.

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
- `MassTransit` `9.0.1`
- `MassTransit.RabbitMQ` `9.0.1`
- `Microsoft.AspNetCore.Authentication.JwtBearer` `8.0.23`
- SignalR server icin ek NuGet zorunlu degil (`Microsoft.AspNetCore.App` ile gelir).
- Scale-out opsiyonu (ileri asama): `Microsoft.AspNetCore.SignalR.StackExchangeRedis`

## Sonraki Plan
- DTO ayirma katmani kullanici istegine gore en sona birakildi.
- PKG-014 UI asamasinda SignalR client abonelikleri ve paging/virtualization baglanacak.
