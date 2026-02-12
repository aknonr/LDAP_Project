# EnterpriseADPasswordManager (LDAP_Project)

Bu repo, kurumsal AD sifre degistirme ve servis hesabi kullanimini yonetmek icin **Onion Architecture + Worker Services + Event Driven** yaklasimiyla ilerliyor.

## Hedef Teknoloji
- `.NET 8`
- `SQL Server 2022`
- `RabbitMQ + MassTransit`
- `SignalR`
- `Serilog`

## Katmanlar
- `Domain`: Entity + enum
- `Application`: Use-case + abstraction
- `Infrastructure`: EF Core + messaging + security + audit/tracking implementasyonlari
- `API`: OIDC + RBAC + endpoint + SignalR + audit rapor
- `Worker`: command consumer + result event publisher
- `Document`: proje genel dokumanlari

## Katman Dokumanlari
- `Domain/Domain_readme.txt`
- `Application/Application_readme.txt`
- `Infrastructure/Infrastructure_readme.txt`
- `API/API_README.txt`
- `Worker/Worker_readme.txt`

## Genel Dokumanlar
- `Document/Config_and_Secrets_Readme.txt`
- `Document/Logging_Redaction_Readme.txt`
- `Document/Messaging_Topology_Readme.txt`
- `Document/API_Contract_Readme.txt`
- `Document/Realtime_Updates_Readme.txt`
- `Document/Audit_and_Logging_Readme.txt`
- `Document/Security_Checklist_Readme.txt`
- `Document/Th_Api_Inventory_Readme.txt`
- `Document/Ad_Ldaps_Change_Readme.txt`
- `Document/Discovery_Engine_Readme.txt`
- `Document/Update_Engine_Readme.txt`
- `Document/roadmap.json`

## Tamamlanan Asamalar
- `PKG-002`: Config/secrets + Serilog iskeleti
- `PKG-003`: EF Core model + DbContext iskeleti
- `PKG-004`: OIDC placeholder + group allowlist + RBAC claims transform
- `PKG-005`: TH API inventory sync (client + worker job)
- `PKG-006`: LDAPS password change servisi
- `PKG-007`: Discovery engine (plugin tabanli iskelet)
- `PKG-008`: Update engine (idempotent iskelet)
- `PKG-009/010`: MassTransit + RabbitMQ topology + Worker consumer iskeleti
- `PKG-011 Adim 3`: API Controller + DI + MQ publish
- `PKG-012`: SignalR hub + API result consumer + canli event akisi
- `PKG-013`: Correlation middleware + request logging enrich + security headers + audit trail/rapor endpoint

## Mimari Baglanti (PKG-012/013)
1. API job olusturur ve command queue'ya yollar.
2. Worker command'i tuketir ve result event publish eder.
3. API result event'i tuketir, DB'de job/target durumunu gunceller.
4. API SignalR hub ile `jobUpdated`/`targetUpdated` eventlerini istemcilere yayinlar.
5. API audit kaydini DB'ye yazar; `Admin/SuperAdmin` audit log endpointinden raporlar.

## src/ Tasima Notu
- Roadmap'te `src/<layer>/<project>` onerisi var.
- Mevcut kok dizin yapisi teknik olarak calisir; zorunlu degil.
- Build pipeline ve referanslar stabil olduktan sonra tasimak daha dogru olur.

## Sonraki Asamalar
1. Service disindaki update stratejilerinin gercek implementasyonu
2. Verify akisinin gercek implementasyonu
3. PKG-014: UI + SignalR istemci entegrasyonu
4. DTO ayirma katmani (kullanici talebiyle en son)

## Guncel Backend Notu
- Service, ScheduledTask, IIS ve COM+ discovery stratejileri WinRM/PowerShell ile aktiflestirildi.
- Service update stratejisi idempotent + hata kodu map ile aktiflestirildi.

## Troubleshooting Notu
- `dotnet list ... --vulnerable` komutu bu ortamda tekrar calistirildi ve basarili.
- `hostpolicy.dll/SDK` problemi su an yeniden uretilemiyor.
- Dogrulama:
  - `dotnet --info` => calisiyor
  - `where dotnet` => `C:\Program Files\dotnet\dotnet.exe`
  - `C:\Program Files\dotnet\host\fxr` altinda `8.0.24` ve `9.0.13` mevcut
  - `dotnet list LDAP_Project.sln package --vulnerable` => tum projelerde acik yok
