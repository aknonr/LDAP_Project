# Messaging Topology (RabbitMQ + MassTransit)

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

Bu dokuman PKG-009/010 ve PKG-012 kapsaminda gunceldir.

## Queue ve Exchange
- Command queue:
  - `ad.passwordchange.commands`
  - `server.discovery.commands`
  - `server.update.commands`
  - `server.verify.commands`
- Result exchange/queue:
  - `server.result.events`

## Message Sozlesmeleri
- Command:
  - `StartPasswordChangeJobCommand`
  - `DiscoverServerUsageCommand`
  - `UpdateServerResourcesCommand`
  - `VerifyServerCommand`
- Event:
  - `ServerUsageResultEvent`
  - `ServerUpdateResultEvent`
  - `JobProgressEvent`

## Akis
1. API command publish eder.
2. Worker command consume eder.
3. Worker result event publish eder (`server.result.events`).
4. API result event consume eder.
5. API DB status update + SignalR broadcast yapar.

### Password-Change Orkestrasyonu (PKG-016)
1. API `StartPasswordChangeJobCommand` publish eder (`ad.passwordchange.commands`).
2. Worker AD (LDAPS) change yapar (old+new).
3. AD basariliysa Worker `UpdateServerResourcesCommand` mesajlarini hedefler icin dispatch eder.
4. Update basariliysa (opsiyonel) Worker `VerifyServerCommand` zincirler.

## Runtime Ayarlari
- RabbitMQ:
  - `Messaging:RabbitMq:Host`
  - `Messaging:RabbitMq:Port`
  - `Messaging:RabbitMq:VirtualHost`
  - `Messaging:RabbitMq:Username`
  - `Messaging:RabbitMq:Password`
  - `Messaging:RabbitMq:UseTls`
  - `Messaging:RabbitMq:SslServerName`
  - `Messaging:RabbitMq:RequestedHeartbeat`
  - `Messaging:RabbitMq:UseQuorumQueues`
  - `Messaging:RabbitMq:QuorumReplicationFactor`
- Outbox (dayaniklilik):
  - `Messaging:Outbox:Enabled`
  - `Messaging:Outbox:UseBusOutbox`
  - `Messaging:Outbox:QueryDelaySeconds`
- Consumer:
  - `Messaging:Consumer:PrefetchCount`
  - `Messaging:Consumer:ConcurrencyLimit`
- Worker role:
  - `WorkerRoles:EnableDiscovery`
  - `WorkerRoles:EnableUpdate`
  - `WorkerRoles:EnableVerify`
  - `WorkerRoles:EnableInventorySync`
- Verify zinciri:
  - `Verification:EnablePostUpdateVerification`

## Guvenlik
- TLS port `5671` zorunlu.
- Sifre payload'i plain degil, AES-GCM sifreli olarak tasinir.
- Retry default: 3 deneme, 5 saniye.
- Quorum icin `QuorumReplicationFactor` ortam bazli ayarlanmalidir (dev: 1, prod: 3).

## Not
- `UpdateServerResourcesCommand` artik `TargetAccount` alanini tasir.
- `VerifyServerCommand` artik `TargetAccount` alanini tasir.
- Quorum queue aciksa command queue'lar durable ve HA davranir.
- Outbox aktifse DB'de `InboxState/OutboxMessage/OutboxState` tablolari migration ile olusur ve publish/consume dayanikliligi artar.
- Multi-instance duplicate koruma icin command publish/dipatch adimlarinda deterministic `MessageId` kullanilir (job veya job+target bazli).

## Hardening Update (2026-02-24)

### Neden Yapildi
- Endpoint tipleri farkli workload'a sahip oldugu icin tek `Prefetch/Concurrency` ayari operasyonel olarak yetersiz kalabiliyordu.
- `InProgress` gorunurlugu yalnizca zincirin belirli adimlarinda olustugu icin stuck tespiti zorlasiyordu.

### Nasil Calisiyor
- Endpoint bazli ayar:
  - `Messaging:Consumer:EndpointOverrides:Discovery`
  - `Messaging:Consumer:EndpointOverrides:PasswordChange`
  - `Messaging:Consumer:EndpointOverrides:Update`
  - `Messaging:Consumer:EndpointOverrides:Verify`
  - API tarafinda `Messaging:Consumer:EndpointOverrides:ResultEvents`
- `UpdateServerResourcesConsumer` ve `VerifyServerConsumer` is basinda `ServerUpdateResultEvent(Status=InProgress)` publish eder.

### Ornek Config
```json
"Messaging": {
  "Consumer": {
    "PrefetchCount": 50,
    "ConcurrencyLimit": 50,
    "EndpointOverrides": {
      "Discovery": { "PrefetchCount": 12, "ConcurrencyLimit": 6 },
      "PasswordChange": { "PrefetchCount": 8, "ConcurrencyLimit": 4 },
      "Update": { "PrefetchCount": 60, "ConcurrencyLimit": 30 },
      "Verify": { "PrefetchCount": 30, "ConcurrencyLimit": 15 },
      "ResultEvents": { "PrefetchCount": 100, "ConcurrencyLimit": 50 }
    }
  }
}
```

### Operasyon / Runbook Notlari
- Tuning degisiklikleri queue bazli backlog metric ile birlikte uygulanmalidir.
- `Update` concurrency yukseltilirken remote endpoint (WinRM) saturation etkisi mutlaka izlenmelidir.

### Risk / Trade-off
- Agresif prefetch/concurrency DB ve remote endpoint'i bogabilir.
- Dusuk ayarlar backlog'un buyumesine neden olabilir.

### Rollback Etkisi
- `EndpointOverrides` silinirse tum endpointler global `Messaging:Consumer` degerlerine geri doner.
