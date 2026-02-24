# Worker Katmani

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Amac
- Command queue'larini tuketir, islem sonuc event'lerini publish eder.

## Mevcut Icerik
- Consumer'lar:
  - `DiscoverServerUsageConsumer`
  - `StartPasswordChangeJobConsumer`
  - `UpdateServerResourcesConsumer`
  - `VerifyServerConsumer`
- Inventory sync:
  - `InventorySyncJob` (periyodik TH API sync)
- Discovery/Update engine:
  - `DiscoveryEngine` ve `UpdateEngine` Worker tarafindan cagrilir
  - Service/ScheduledTask/IIS/COM+ discovery WinRM/PowerShell ile calisir
  - Service/ScheduledTask/IIS/COM+ update WinRM/PowerShell ile calisir
- Verify:
  - Update sonrasi (opsiyonel) verify komutu gonderilir
  - Config: `Verification:EnablePostUpdateVerification`
- Worker role dagitimi:
  - `WorkerRoles:EnableDiscovery`
  - `WorkerRoles:EnableUpdate`
  - `WorkerRoles:EnableVerify`
  - `WorkerRoles:EnableInventorySync`
- RabbitMQ + MassTransit:
  - Queue consume: `ad.passwordchange.commands`, `server.discovery.commands`, `server.update.commands`, `server.verify.commands`
  - Result publish exchange: `server.result.events`
  - Quorum queue (opsiyonel): `UseQuorumQueues`
  - EF Outbox (opsiyonel): `Messaging:Outbox`
  - Deterministic MessageId: `update/verify` dispatch'lerinde stabil `MessageId` ile inbox dedupe yardimi
- Concurrency:
  - `PrefetchCount` ve `ConcurrencyLimit` configten okunur.
  - Retry: 3 deneme, 5 saniye aralik

## SignalR ile Baglanti
- Worker dogrudan SignalR'a cikmaz.
- Worker event publish eder, API event consumer alir, sonra SignalR hub broadcast eder.

## LDAPS Notu
- Worker config'inde `Ldap` ayarlari bulunur (PKG-006).
- AD sifre degistirme servisi, password-change job orkestrasyonu icinde calisir:
  - `StartPasswordChangeJobCommand` consume edilir
  - AD (LDAPS) change (old+new) -> update -> (opsiyonel) verify zinciri baslar

## Sonraki Plan
- Resilience hardening: DLQ + circuit-breaker/kill-switch (PKG-017).
- Key rotation / multi-key decrypt (PKG-023).

## Yuk Dagitimi ve Failover
- Ayni queue'yu birden fazla worker instance tukettiginde `competing consumers` modeli ile yuk dagitilir.
- Bir worker instance duserse, mesajlar queue'da kalir ve ayakta olan worker'lar tuketmeye devam eder.
- Ayni target icin tekrar dispatch durumunda worker tarafi deterministic `MessageId` + DB unique index ile cift kayit riskini azaltir.
- Is rol bazli bolunebilir:
  - discovery worker: `EnableDiscovery=true`
  - update worker: `EnableUpdate=true`
  - verify worker: `EnableVerify=true`
- Yatay olcekleme icin her rolun instance sayisi ayri arttirilabilir.

## Hardening Update (2026-02-24)

### Neden Yapildi
- Multi-instance production'da `InventorySyncJob`'un tekil calismasi garanti degildi.
- Tum consumer endpoint'lerinin ayni concurrency ayariyla calismasi queue starvation riski olusturuyordu.
- Gec gelen/tekrar event'lerin state overwrite etmesi ve hedeflerin uzun sure `InProgress` kalmasi operasyon riskiydi.
- Windows Service production operasyonunda health/queue lag gorunurlugu artirilmak istendi.

### Nasil Calisiyor
- `InventorySyncJob`, DB lease almadan sync calistirmaz:
  - `DistributedLeases` tablosu
  - `IDistributedLeaseManager` + `SqlDistributedLeaseManager`
  - acquire/renew/release dongusu
- `UpdateServerResourcesConsumer` ve `VerifyServerConsumer`, is basinda `ServerUpdateResultEvent(InProgress)` publish eder.
- Consumer tuning endpoint bazli override ile yapilir:
  - `Discovery`, `PasswordChange`, `Update`, `Verify`
- Worker observability:
  - `WorkerHealthReporterHostedService` (DB baglantisi + process metrik logu)
  - `QueueLagReporterHostedService` + `RabbitMqManagementQueueLagProbe`
- Worker host explicit Windows Service olarak kaydolur:
  - `AddWindowsService(...)`

### Config Anahtarlari
- Inventory lease:
  - `InventorySyncLease:Enabled`
  - `InventorySyncLease:LeaseName`
  - `InventorySyncLease:LeaseDurationSeconds`
  - `InventorySyncLease:RenewIntervalSeconds`
  - `InventorySyncLease:AcquisitionRetrySeconds`
- Endpoint bazli consumer tuning:
  - `Messaging:Consumer:EndpointOverrides:Discovery:*`
  - `Messaging:Consumer:EndpointOverrides:PasswordChange:*`
  - `Messaging:Consumer:EndpointOverrides:Update:*`
  - `Messaging:Consumer:EndpointOverrides:Verify:*`
- Health:
  - `Observability:Health:Enabled`
  - `Observability:Health:IntervalSeconds`
  - `Observability:Health:CheckDatabase`
- Queue lag:
  - `Observability:QueueLag:Enabled`
  - `Observability:QueueLag:ManagementBaseUrl`
  - `Observability:QueueLag:VirtualHost`
  - `Observability:QueueLag:Username`
  - `Observability:QueueLag:Password`
  - `Observability:QueueLag:IntervalSeconds`
  - `Observability:QueueLag:WarningReadyThreshold`
  - `Observability:QueueLag:WarningUnackedThreshold`
  - `Observability:QueueLag:Queues`

### Operasyon / Runbook Notlari
- Minimum HA: Update+Verify rolu icin en az 2 instance.
- Inventory role (`EnableInventorySync=true`) olan instance sayisi artsa bile lease sebebiyle tek instance aktif sync yapar.
- Windows Service recovery ornegi:
  - `sc.exe failure "EnterpriseADPasswordManager.Worker" reset= 86400 actions= restart/5000/restart/15000/restart/60000`
  - `sc.exe failureflag "EnterpriseADPasswordManager.Worker" 1`
- Queue lag alarminda once backlog nedeni ayrisimi yap:
  - remote endpoint saturation
  - DB bottleneck
  - downstream timeout/firewall

### Risk / Trade-off
- DB lease yazimlari ek DB yuk olusturur.
- Queue lag metric icin RabbitMQ Management API erisimi/accreditation gerekir.
- Endpoint override tuning ortamdan ortama degisebilir; ilk rollout kontrollu yapilmalidir.

### Rollback Etkisi
- `InventorySyncLease:Enabled=false` ile lease davranisi kapatilabilir.
- `Observability:QueueLag:Enabled=false` ile queue lag reporter kapatilabilir.
- `Messaging:Consumer:EndpointOverrides` bloklari silinirse global ayarlar kullanilir.
- Kod rollback gerekirse migration geri alinmadan once `DistributedLeases` tablosuna bagimli service davranislari devre disi birakilmalidir.

### Paketler (Kesin Surumler)
- `Worker/Worker.csproj`:
  - `MassTransit` `8.4.1`
  - `MassTransit.EntityFrameworkCore` `8.4.1`
  - `MassTransit.RabbitMQ` `8.4.1`
  - `Microsoft.Extensions.Hosting` `8.0.1`
  - `Microsoft.Extensions.Hosting.WindowsServices` `8.0.1` (yeni)
  - `Serilog.Extensions.Hosting` `10.0.0`
  - `Serilog.Settings.Configuration` `10.0.0`
  - `Serilog.Sinks.EventLog` `4.0.0`
  - `Serilog.Sinks.File` `7.0.0`
