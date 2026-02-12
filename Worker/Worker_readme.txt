# Worker Katmani

## Amac
- Command queue'larini tuketir, islem sonuc event'lerini publish eder.

## Mevcut Icerik
- Consumer'lar:
  - `DiscoverServerUsageConsumer`
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
  - Queue consume: `server.discovery.commands`, `server.update.commands`, `server.verify.commands`
  - Result publish exchange: `server.result.events`
  - Quorum queue (opsiyonel): `UseQuorumQueues`
  - EF Outbox (opsiyonel): `Messaging:Outbox`
- Concurrency:
  - `PrefetchCount` ve `ConcurrencyLimit` configten okunur.
  - Retry: 3 deneme, 5 saniye aralik

## SignalR ile Baglanti
- Worker dogrudan SignalR'a cikmaz.
- Worker event publish eder, API event consumer alir, sonra SignalR hub broadcast eder.

## LDAPS Notu
- Worker config'inde `Ldap` ayarlari bulunur (PKG-006).
- AD sifre degistirme servisi mevcut; password-change job akisi ile orkestrasyon sonraki adimdir.

## Sonraki Plan
- UserRight discovery stratejisinin gercek implementasyonu.
- Password-change job icin AD (LDAPS) change + update + verify orkestrasyonu.

## Yuk Dagitimi ve Failover
- Ayni queue'yu birden fazla worker instance tukettiginde `competing consumers` modeli ile yuk dagitilir.
- Bir worker instance duserse, mesajlar queue'da kalir ve ayakta olan worker'lar tuketmeye devam eder.
- Is rol bazli bolunebilir:
  - discovery worker: `EnableDiscovery=true`
  - update worker: `EnableUpdate=true`
  - verify worker: `EnableVerify=true`
- Yatay olcekleme icin her rolun instance sayisi ayri arttirilabilir.
