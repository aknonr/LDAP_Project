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
- Consumer:
  - `Messaging:Consumer:PrefetchCount`
  - `Messaging:Consumer:ConcurrencyLimit`

## Guvenlik
- TLS port `5671` zorunlu.
- Sifre payload'i plain degil, AES-GCM sifreli olarak tasinir.
- Retry default: 3 deneme, 5 saniye.

## Not
- `UpdateServerResourcesCommand` artik `TargetAccount` alanini tasir.
