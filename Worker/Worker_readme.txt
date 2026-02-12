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
  - Service discovery WinRM/PowerShell transportu ile calisir
- Worker role dagitimi:
  - `WorkerRoles:EnableDiscovery`
  - `WorkerRoles:EnableUpdate`
  - `WorkerRoles:EnableVerify`
  - `WorkerRoles:EnableInventorySync`
- RabbitMQ + MassTransit:
  - Queue consume: `server.discovery.commands`, `server.update.commands`, `server.verify.commands`
  - Result publish exchange: `server.result.events`
- Concurrency:
  - `PrefetchCount` ve `ConcurrencyLimit` configten okunur.
  - Retry: 3 deneme, 5 saniye aralik

## SignalR ile Baglanti
- Worker dogrudan SignalR'a cikmaz.
- Worker event publish eder, API event consumer alir, sonra SignalR hub broadcast eder.

## LDAPS Notu
- Worker config'inde `Ldap` ayarlari bulunur (PKG-006).
- Update engine entegre oldugunda bu servis kullanilacak.

## Sonraki Plan
- Service disindaki update stratejilerini gercek implementasyona tasima.

## Yuk Dagitimi ve Failover
- Ayni queue'yu birden fazla worker instance tukettiginde `competing consumers` modeli ile yuk dagitilir.
- Bir worker instance duserse, mesajlar queue'da kalir ve ayakta olan worker'lar tuketmeye devam eder.
- Is rol bazli bolunebilir:
  - discovery worker: `EnableDiscovery=true`
  - update worker: `EnableUpdate=true`
  - verify worker: `EnableVerify=true`
- Yatay olcekleme icin her rolun instance sayisi ayri arttirilabilir.
