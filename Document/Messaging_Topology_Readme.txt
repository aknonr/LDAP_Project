# Messaging Topology (RabbitMQ + MassTransit)

Bu dokuman PKG-009/010 kapsaminda olusturuldu.

## Queue Adlari
- `server.discovery.commands`
- `server.update.commands`
- `server.verify.commands`
- `server.result.events`

## Command/Event Sozlesmeleri
Command:
- `DiscoverServerUsageCommand`
- `UpdateServerResourcesCommand`
- `VerifyServerCommand`

Event:
- `ServerUsageResultEvent`
- `ServerUpdateResultEvent`
- `JobProgressEvent`

## Topology Notlari
- Command queue'lari Worker tarafinda tüketilir.
- Result event’leri `server.result.events` entity adiyla publish edilir.
- TLS 5671 zorunlu (internal).

## Konfigurasyon Anahtarlari
API ve Worker icin:
- `Messaging:RabbitMq:Host`
- `Messaging:RabbitMq:Port`
- `Messaging:RabbitMq:VirtualHost`
- `Messaging:RabbitMq:Username`
- `Messaging:RabbitMq:Password`
- `Messaging:RabbitMq:UseTls`
- `Messaging:RabbitMq:SslServerName`
- `Messaging:RabbitMq:RequestedHeartbeat`

Worker ek ayar:
- `Messaging:Consumer:PrefetchCount`
- `Messaging:Consumer:ConcurrencyLimit`
