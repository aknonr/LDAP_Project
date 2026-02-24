# Domain Katmanı

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

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
