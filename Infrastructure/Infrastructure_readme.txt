# Infrastructure Katmani

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Amac
- DB erisimi, dis sistem entegrasyonu, messaging ve guvenlik implementasyonlari burada toplaniyor.

## Mevcut Icerik
- EF Core:
  - `AdpmDbContext`
  - Paketler: `Microsoft.EntityFrameworkCore`, `SqlServer`, `Design`, `Tools` => `8.0.23`
- TH API:
  - `ThApiClient` (HTTP client)
  - `InventorySyncService`
- Repository:
  - `JobRepository`
  - `ServerGroupRepository`
  - `IdentityRepository` (AppUser/Role yonetimi)
- Messaging:
  - `MassTransitCommandPublisher`
  - `DeterministicMessageIdFactory`
  - `RabbitMqOptions`, `ConsumerOptions`, `OutboxOptions`
  - `MassTransitTopologyExtensions` (quorum/outbox endpoint defaults)
- Security:
  - `AesGcmPayloadProtector`
  - `SensitiveDataRedactor`
- Directory:
  - `AdPasswordChangeService` (LDAPS change)
- Discovery/Update:
  - `DiscoveryEngine` + discovery strategies
  - `UpdateEngine` + update strategies
- Verify:
  - `VerifyEngine` (update sonrasi dogrulama)
- Remote execution:
  - `PowerShellWinRmCommandExecutor`
  - `RemoteExecutionOptions`
- Tracking:
  - `JobTrackingService` (result event -> Job/Target status update)
- Audit:
  - `AuditTrailStore` (writer + reader)

## Guvenlik Notlari
- MQ payload sifreleme `AES-GCM`; plain sifre DB/log/MQ'ya yazilmaz.
- Audit metinleri sanitize edilir; hassas anahtarlar redacted yazilir.
- CorrelationId job ve audit kayitlarina tasinabilir.

## Multi-Instance Hardening
- Mesaj tekrarlarinda `MessageId` stabil kalacak sekilde deterministic uretilir:
  - `start-password-change`, `discovery`, `update`, `verify`
- Worker birden fazla instance calistiginda `InboxState (MessageId+ConsumerId)` ile duplicate consume riski azaltilir.
- DB tarafinda cift kayit korumasi:
  - `JobTargets(JobId, ServerName)` unique
  - `JobResources(JobTargetId, ResourceType, ResourceName, ResourcePath)` unique
- `UpdateServerResourcesConsumer` kaynak hazirlarken unique conflict yakalar ve mevcut kayitlarla devam eder.

## Baglantilar
- `IJobTrackingService` implementasyonu API consumer'larindan cagrilir.
- `IAuditTrailWriter/Reader` implementasyonu API controller'larinda kullanilir.

## Sonraki Plan
- PKG-019: User/Role yonetimi: soft delete + audit raporlama (tamamlama)
