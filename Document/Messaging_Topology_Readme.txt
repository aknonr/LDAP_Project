# Messaging Topology (RabbitMQ + MassTransit)

Bu dokuman PKG-009/010 ve PKG-012 kapsaminda gunceldir.

## Queue ve Exchange
- Command queue:
  - `server.discovery.commands`
  - `server.update.commands`
  - `server.verify.commands`
- Result exchange/queue:
  - `server.result.events`

## Message Sozlesmeleri
- Command:
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
