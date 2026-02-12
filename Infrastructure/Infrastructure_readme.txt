# Infrastructure Katmani

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
- Messaging:
  - `MassTransitCommandPublisher`
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

## Baglantilar
- `IJobTrackingService` implementasyonu API consumer'larindan cagrilir.
- `IAuditTrailWriter/Reader` implementasyonu API controller'larinda kullanilir.

## Sonraki Plan
- UserRight discovery stratejisinin gercek implementasyonu
- Password-change job orkestrasyonu: AD (LDAPS) change + update + verify akisini tek job altinda tamamlamak
- Mesajlasma hardening: DLQ policy + circuit-breaker/kill-switch config opsiyonlari
