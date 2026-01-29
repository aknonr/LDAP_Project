# Domain Katmanı

## Amaç
Saf iş modeli ve enumlar bu katmanda yer alır. Dış bağımlılık **yoktur**.

## Mevcut İçerik
- Entity’ler: `AppUser`, `Role`, `AppUserRole`, `Job`, `JobTarget`, `JobResource`, `ServerInventory`, `ServerGroup`, `AuditLog`, `OutboxMessage`
- Enumlar: `JobType`, `JobStatus`, `TargetStatus`, `ResourceType`

## Notlar
- Domain katmanında **şifre / secret** gibi alanlar bulunmaz.
- Tüm timestamp alanları UTC olarak tasarlanır.

## Sonraki Aşamalar
- Domain doğrulama kuralları ve value object’ler
- Domain event’leri (outbox ile entegre)
