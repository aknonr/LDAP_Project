# Infrastructure Katmanı

## Amaç
DB erişimi, dış sistem entegrasyonları ve messaging altyapısı bu katmanda toplanır.

## Mevcut İçerik
- `AdpmDbContext` (EF Core)
- EF Core paketleri (.NET 8 uyumlu en güncel 8.x sürümleri)

## Güvenlik Notları
- Payload şifreleme anahtarı config’te **placeholder** olarak tutulur.
- Şifreler DB veya loglarda tutulmaz.

## Sonraki Aşamalar
- EF Core migration üretimi (`PKG-003`)
- LDAP/LDAPS servisleri (`PKG-006`)
- RabbitMQ + MassTransit altyapısı (`PKG-009`)
