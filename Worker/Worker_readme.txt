# Worker Katmanı

## Amaç
Discovery / update / verify işleri ve MQ consumer’ları Worker servisinde çalışır.

## Mevcut İçerik
- Worker Service iskeleti
- Serilog loglama iskeleti
- MassTransit consumer’ları ve RabbitMQ baglanti ayarlari
- Prefetch ve concurrency ayarlari configten okunur

## Sonraki Aşamalar
- Discovery / Update job akışları (`PKG-007`, `PKG-008`)
