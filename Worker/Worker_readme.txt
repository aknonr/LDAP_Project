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
