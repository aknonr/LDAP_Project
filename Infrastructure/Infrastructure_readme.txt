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
  - `RabbitMqOptions`, `ConsumerOptions`
- Security:
  - `AesGcmPayloadProtector`
  - `SensitiveDataRedactor`
- Directory:
  - `AdPasswordChangeService` (LDAPS change)
- Discovery/Update:
  - `DiscoveryEngine` + discovery strategies
  - `UpdateEngine` + update strategies
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
- ScheduledTask/IIS/COM+ update stratejilerinin WinRM/PowerShell ile gercek implementasyonu
- UserRight discovery stratejisinin gercek implementasyonu
- Verify akisinin gercek implementasyonu ve hata kodu standardizasyonu
