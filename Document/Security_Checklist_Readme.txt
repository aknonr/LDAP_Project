# Security Checklist

## Uygulama Katmani
- [x] OIDC bearer validation aktif
- [x] Group allowlist policy aktif
- [x] RBAC claim transform DB tabanli
- [x] Fallback policy ile auth zorunlu

## API Katmani
- [x] `X-Correlation-Id` standardi
- [x] Serilog structured request logging
- [x] Security headers (`nosniff`, `deny`, CSP, referrer policy)
- [x] Swagger sadece development ortami
- [x] Password alanlari loglanmiyor

## Messaging
- [x] RabbitMQ TLS (`5671`) yapisi
- [x] Quorum queue secenegi (HA icin)
- [x] Prefetch/concurrency config bazli
- [x] Retry politikasi aktif
- [x] Result event topology sabit exchange adi
- [x] EF Outbox (publish/consume dayaniklilik)

## Data ve Secrets
- [x] AES-GCM payload encryption
- [x] Secret placeholder'lar appsettings'te
- [x] Audit trail DB kaydi
- [x] LDAPS sadece 636 (config enforce)
- [ ] Secret store entegrasyonu (Vault/Credential Manager) production hardening asamasinda

## Operasyon
- [x] API ve Worker ayri deploy edilebilir
- [x] Event-driven akis mevcut, polling yok
- [x] Remote execution timeout kontrolu (`RemoteExecution`)
- [x] Worker role bazli queue tuketimi (`WorkerRoles`)
- [ ] SIEM forwarding opsiyonel entegrasyon planlandi
