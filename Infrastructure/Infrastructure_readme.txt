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

## Hardening Update (2026-02-24)

### Neden Yapildi
- Multi-instance senaryoda singleton isler icin in-memory lock yeterli degildi.
- Job/target state update tarafinda gec gelen event overwrite riski vardi.
- Consumer endpoint bazli tuning ihtiyaci vardi.

### Nasil Calisiyor
- Yeni distributed lease altyapisi:
  - `Domain/Entities/DistributedLease.cs`
  - `Infrastructure/Concurrency/IDistributedLeaseManager.cs`
  - `Infrastructure/Concurrency/SqlDistributedLeaseManager.cs`
  - `Infrastructure/Migrations/20260224203055_AddDistributedLease.cs`
- DB model:
  - `DistributedLeases` tablosu (`Name` PK, `LeaseUntilUtc`, `RowVersion`)
- Consumer tuning:
  - `ConsumerOptions.EndpointOverrides` + `ConsumerOptionsExtensions.ResolveForEndpoint(...)`
- State overwrite korumasi:
  - `JobTrackingService` icinde stale/out-of-order guard
  - terminal state'ten non-terminal state'e donus engeli

### Config Anahtarlari
- `Messaging:Consumer:EndpointOverrides:<EndpointName>:PrefetchCount`
- `Messaging:Consumer:EndpointOverrides:<EndpointName>:ConcurrencyLimit`
- `Messaging:Consumer:EndpointOverrides:<EndpointName>:RetryAttempts`
- `Messaging:Consumer:EndpointOverrides:<EndpointName>:RetryIntervalSeconds`
- `Messaging:Consumer:EndpointOverrides:<EndpointName>:KillSwitch:*`

### Operasyon / Runbook Notlari
- Yeni migration uygulanmadan lease tabanli singleton servisler aktif edilmemelidir.
- SQL clock drift olmasi lease davranisini etkileyebilir; NTP senkronizasyonu zorunlu tutulmalidir.
- Lease tablosunda stale kayit birikimi beklenmez (release ile silinir), ancak operasyonel izleme yapilmalidir.

### Risk / Trade-off
- Lease acquire/renew/release adimlari DB round-trip sayisini arttirir.
- Terminal lock kurali nedeniyle cok gec gelen farkli status eventleri bilincli olarak drop edilir.

### Rollback Etkisi
- Migration rollback ile `DistributedLeases` tablosu kalkar.
- Kod rollback yapilip migration geri alinmazsa lease manager kullanan servisler runtime'da hata alir.
- Endpoint override rollback'i config bazli yapilabilir (`EndpointOverrides` bloklarini kaldir).

### Paketler (Kesin Surumler)
- `Infrastructure/Infrastructure.csproj`:
  - `MassTransit` `8.4.1`
  - `MassTransit.EntityFrameworkCore` `8.4.1`
  - `MassTransit.RabbitMQ` `8.4.1`
  - `Microsoft.EntityFrameworkCore` `8.0.23`
  - `Microsoft.EntityFrameworkCore.Design` `8.0.23`
  - `Microsoft.EntityFrameworkCore.SqlServer` `8.0.23`
  - `Microsoft.EntityFrameworkCore.Tools` `8.0.23`
  - `Microsoft.Extensions.Http` `8.0.1`
  - `System.DirectoryServices.Protocols` `10.0.3`
