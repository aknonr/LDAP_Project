# EnterpriseADPasswordManager (LDAP_Project)

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

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
- `Document/Reliability_and_Error_Handling_Readme.txt`
- `Document/Audit_and_Logging_Readme.txt`
- `Document/Security_Checklist_Readme.txt`
- `Document/User_Role_Management_Readme.txt`
- `Document/Th_Api_Inventory_Readme.txt`
- `Document/Ad_Ldaps_Change_Readme.txt`
- `Document/Discovery_Engine_Readme.txt`
- `Document/Update_Engine_Readme.txt`
- `Document/Verify_Flow_Readme.txt`
- `Document/agent_handoff.json` (baska bir AI ile devam icin, mevcut durum + eksikler + sonraki adimlar)
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
- `PKG-015`: UserRight discovery stratejisi (WinRM/PowerShell + secedit export)
- `PKG-016`: Password-change orkestrasyonu (AD change -> update -> verify)
- `PKG-017`: Resilience hardening (retry + kill-switch + circuit-breaker)
- `PKG-018`: Authorization hardening (DB allowlist + job/hub authz)
- `PKG-019 (kismi)`: Admin user/role API + RBAC seed/bootstrap

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
1. `PKG-019`: User/Role yonetimi: soft delete + audit raporlama (tamamlama)
2. `PKG-014`: UI + SignalR istemci entegrasyonu (Phase 2)
3. `PKG-020`: Permission modeli (fine-grained authorization)
4. `PKG-021`: Observability v2 (OpenTelemetry + healthchecks + rate limiting)
5. `PKG-022`: SignalR scale-out (backplane) + deployment guide
6. `PKG-023`: Key rotation + secret/certificate rotation runbook
7. DTO ayirma katmani (kullanici talebiyle en son)

## PKG-014 (UI) Detaylari (Phase 2)
- UI stack: `TBD` (React veya Blazor). Roadmap bunu bilerek acik birakiyor.
- Sayfalar: `Dashboard`, `Discovery`, `Password Change Form`, `Job Detail`, `Unauthorized`.
- Liste performansi: 2000+ target icin `paging + virtualization` zorunlu.
- Tab ayrimi: `Basarili` / `Basarisiz` (target listesinde filtre).
- Kolonlar: `Server`, `Status`, `ResourceType`, `ErrorCode`, `ErrorMessage`, `UpdatedAt`.
- Yetki bazli UI guard: Viewer teknik detaylari gorememeli; Operator/Admin job baslatabilmeli.
- Realtime: SignalR `jobUpdated` / `targetUpdated` eventleri ile canli guncelleme; ama kaynak-of-truth REST/DB.
- Deploy: UI ve API ayri origin olacagi icin API tarafinda `Cors:AllowedOrigins` ayari yapilacak.
- Windows/IIS notu: SignalR sunucu kurulumu yok; IIS host edilecekse `WebSocket Protocol` acik olmali (detay: `Document/Realtime_Updates_Readme.txt`).

## Kurumsal Backlog (PKG-018+)
- `PKG-018 (tamamlandi)`: Authorization hardening: DB allowlist (kullanici DB'de yoksa 403) + `jobId` bazli erisim kontrolu + hub subscription authorization.
- `PKG-019`: User/Role yonetimi: Admin/SuperAdmin API + soft delete (`IsActive=false`) + rol atama/geri alma.
- `PKG-020`: Permission modeli: sadece `Role` degil, ince yetkiler icin `Permission` tablosu/policy modeli.
- `PKG-021`: Observability v2: OpenTelemetry (trace/metric) + healthcheck + rate limiting.
- `PKG-022`: SignalR scale-out: API scale-out senaryosunda backplane (or. Redis) veya sticky-session karari + deploy guide.
- `PKG-023`: Key rotation: AES-GCM shared key icin `KeyId` rotation plani ve sertifika/secret yenileme runbook'u.

## Guncel Backend Notu
- Service, ScheduledTask, IIS ve COM+ discovery stratejileri WinRM/PowerShell ile aktiflestirildi.
- Service update stratejisi idempotent + hata kodu map ile aktiflestirildi.
- ScheduledTask/IIS/COM+ update stratejileri WinRM/PowerShell ile aktiflestirildi.
- Verify akisi aktif: update sonrasi (opsiyonel) verify komutu gonderilir ve final durum event ile UI'ya yansir.
- RabbitMQ quorum queue + EF outbox hardening eklendi (HA + publish/consume dayanikliligi).
- Multi-instance duplicate koruma: deterministic `MessageId` + `JobTargets/JobResources` unique index + update hazirlik adiminda unique-conflict fallback.
- WinRM hata loglari firewall/network teshisi icin detaylandirildi (`ErrorSummary` + `Hint` + timeout/transport alanlari).

## Troubleshooting Notu
- `dotnet list ... --vulnerable` komutu bu ortamda tekrar calistirildi ve basarili.
- `hostpolicy.dll/SDK` problemi su an yeniden uretilemiyor.
- Dogrulama:
  - `dotnet --info` => calisiyor
  - `where dotnet` => `C:\Program Files\dotnet\dotnet.exe`
  - `C:\Program Files\dotnet\host\fxr` altinda `8.0.24` ve `9.0.13` mevcut
  - `dotnet list LDAP_Project.sln package --vulnerable` => tum projelerde acik yok
