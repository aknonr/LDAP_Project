# EnterpriseADPasswordManager (LDAP_Project)

Bu repo, kurumsal AD şifre değiştirme ve servis hesabı kullanımını yönetmek için **Onion Architecture + Worker Services + Event Driven** yaklaşımıyla tasarlanmıştır. Hedef teknoloji seti `.NET 8`, `SQL Server 2022`, `RabbitMQ + MassTransit`, `SignalR`.

## Mimari Özet
- **Domain:** Saf iş modelleri ve enumlar.
- **Application:** Use-case’ler, arayüzler, politika/iş kuralları.
- **Infrastructure:** DB erişimi, dış sistem entegrasyonları, messaging altyapısı.
- **API:** OIDC giriş, RBAC, job başlatma ve izleme uçları.
- **Worker:** Discovery/update/verify işleri; MQ consumer’ları.

Detaylar ve planlar: `Document/roadmap.json`



## Konfigürasyon ve Güvenlik
- Config/secret stratejisi: `Document/Config_and_Secrets_Readme.txt`
- Log redaction politikası: `Document/Logging_Redaction_Readme.txt`
- Messaging topology: `Document/Messaging_Topology_Readme.txt`

## Katman Dökümanları (Güncel Konumlar)
- `Domain/Domain_readme.txt`
- `Application/Application_readme.txt`
- `Infrastructure/Infrastructure_readme.txt`
- `API/API_README.txt`
- `Worker/Worker_readme.txt`

## Bu Aşamada Yapılanlar (PKG-002 + PKG-003 + PKG-004 + PKG-009/010 + PKG-011-2)
- Serilog iskeleti ve config placeholder’ları eklendi.
- EF Core paketleri 8.0.23’e sabitlendi ve **DbContext + entity iskeleti** oluşturuldu.
- OIDC placeholder ayarları `API/appsettings.json` içine alındı.
- JWT Bearer auth + group allowlist (policy) + RBAC claim transformation altyapısı eklendi.
- MassTransit + RabbitMQ topology ve Worker consumer iskeleti eklendi.
- API request/response contract iskeleti eklendi (jobs/auth).
- Application use-case iskeleti eklendi (job create/status/targets).

## Migrations Notu
Migrations oluşturmak için önce gerçek connection string sağlanmalı:
- `ConnectionStrings:Default` placeholder’dan gerçek değere çevrilir.
- `dotnet ef migrations add InitialCreate --project Infrastructure --startup-project API`


## `src/` Klasör Yapısı
Roadmap’e göre önerilen yapı `src/<layer>/<project>` şeklindedir. **Zorunlu değildir**, ancak orta/uzun vadede düzen ve tutarlılık için önerilir. Taşımayı istersen birlikte yaparız.

## Sonraki Aşamalar
1. **PKG-011:** API endpoints (auth + job create/status)
2. **PKG-012:** SignalR canlı güncellemeler
3. **PKG-013:** Observability + audit trail
