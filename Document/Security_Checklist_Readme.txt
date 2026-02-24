# Security Checklist

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Uygulama Katmani
- [x] OIDC bearer validation aktif
- [x] Group allowlist policy aktif
- [x] RBAC claim transform DB tabanli
- [x] Default policy: authenticated + group allowlist + DB allowlist (fail-closed)
- [x] Fallback policy: authorize olmayan endpoint'ler de fail-closed

## API Katmani
- [x] `X-Correlation-Id` standardi
- [x] Serilog structured request logging
- [x] Security headers (`nosniff`, `deny`, CSP, referrer policy)
- [x] Swagger sadece development ortami
- [x] Password alanlari loglanmiyor
- [x] Job/Hub erisim kontrolu: `RequestedBySubject` (Admin/SuperAdmin tum job'lar, diger rollerde sadece kendi job'u)

## Messaging
- [x] RabbitMQ TLS (`5671`) yapisi
- [x] Quorum queue secenegi (HA icin)
- [x] Prefetch/concurrency config bazli
- [x] Retry politikasi aktif
- [x] Result event topology sabit exchange adi
- [x] EF Outbox (publish/consume dayaniklilik)
- [x] Deterministic `MessageId` (job veya job+target bazli) ile duplicate consume riski azaltildi

## Data ve Secrets
- [x] AES-GCM payload encryption
- [x] Secret placeholder'lar appsettings'te
- [x] Audit trail DB kaydi
- [x] DB allowlist: `AppUsers.Subject` must-exist + `IsActive=true` degilse 403
- [x] LDAPS sadece 636 (config enforce)
- [x] DB duplicate korumasi: `JobTargets(JobId,ServerName)` + `JobResources(JobTargetId,ResourceType,ResourceName,ResourcePath)` unique index
- [ ] Secret store entegrasyonu (Vault/Credential Manager) production hardening asamasinda
- [x] Ilk kurulum bootstrap: `Auth:Bootstrap` (opsiyonel) + admin user/role API

## Operasyon
- [x] API ve Worker ayri deploy edilebilir
- [x] Event-driven akis mevcut, polling yok
- [x] Remote execution timeout kontrolu (`RemoteExecution`)
- [x] Worker role bazli queue tuketimi (`WorkerRoles`)
- [x] WinRM hata loglari firewall/network teshisi icin `ErrorSummary` + `Hint` alanlariyla zenginlestirildi
- [ ] SIEM forwarding opsiyonel entegrasyon planlandi
