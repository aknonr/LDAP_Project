# API Katmanı

## Amaç
OIDC login, RBAC, job oluşturma ve izleme endpointleri burada bulunur.

## Mevcut İçerik
- Serilog loglama iskeleti
- OIDC config placeholder (`Auth:Oidc`)
- JWT Bearer auth + group allowlist (policy) altyapısı
- RBAC claim transformation (DB role mapping)
- MassTransit bus konfigurasyonu (RabbitMQ publish)
- Command queue mapping (EndpointConvention)
- PKG-011 icin API DTO/contract iskeleti (Jobs/Auth)

## Sonraki Aşamalar
- OIDC login + group allowlist + RBAC (`PKG-004`)
- Job create/status endpointleri (`PKG-011`)
- SignalR hub ve event köprüsü (`PKG-012`)
