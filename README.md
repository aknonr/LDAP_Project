# EnterpriseADPasswordManager (LDAP_Project)

Bu repo, kurumsal AD şifre değiştirme ve servis hesabı kullanımını yönetmek için **Onion Architecture + Worker Services + Event Driven** yaklaşımıyla tasarlanmıştır. Hedef teknoloji seti `.NET 8`, `SQL Server 2022`, `RabbitMQ + MassTransit`, `SignalR`.

## Mimari Özet
- **Domain:** Saf iş modelleri ve enumlar.
- **Application:** Use-case’ler, arayüzler, politika/iş kuralları.
- **Infrastructure:** DB erişimi, dış sistem entegrasyonları, messaging altyapısı.
- **API:** OIDC giriş, RBAC, job başlatma ve izleme uçları.
- **Worker:** Discovery/update/verify işleri; MQ consumer’ları.

Detaylar ve planlar: `docs/roadmap.json`

## Katman Readme Dosyaları
Her katman için detaylı açıklamalar:
- `docs/Domain_readme.md`
- `docs/Application_readme.md`
- `docs/Infrastructure_readme.md`
- `docs/API_readme.md`
- `docs/Worker_readme.md`

## Konfigürasyon ve Güvenlik
- Config/secret stratejisi: `docs/config-and-secrets.md`
- Log redaction politikası: `docs/logging-redaction-policy.md`

## Bu Aşamada Yapılanlar (PKG-002 + PKG-003)
- Serilog iskeleti ve config placeholder’ları eklendi.
- EF Core paketleri eklendi ve **DbContext + entity iskeleti** oluşturuldu.
- OIDC placeholder ayarları `API/appsettings.json` içine alındı.

## Migrations Notu
Migrations oluşturmak için önce gerçek connection string sağlanmalı:
- `ConnectionStrings:Default` placeholder’dan gerçek değere çevrilir.
- `dotnet ef migrations add InitialCreate --project Infrastructure --startup-project API`

## `src/` Klasör Yapısı
Roadmap’e göre önerilen yapı `src/<layer>/<project>` şeklindedir. **Zorunlu değildir**, ancak orta/uzun vadede düzen ve tutarlılık için önerilir. Taşımayı istersen birlikte yaparız.

## Sonraki Aşamalar
1. **PKG-004:** OIDC login + allowlist + RBAC servisleri
2. **PKG-009/010:** MassTransit + RabbitMQ topology ve Worker consumer’ları
3. **PKG-012:** SignalR canlı güncellemeler
