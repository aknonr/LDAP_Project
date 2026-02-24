# Logging Redaction Politikası

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

Bu politika, loglarda **hiçbir secret/sifre/tokens** bilgisinin bulunmamasını garanti eder.

## Hassas Alanlar (Mutlaka Maskele)
- `oldPassword`
- `newPassword`
- `tokens`
- `secrets`
- OIDC tokenları (access/refresh/id token)
- LDAP/AD kimlik bilgileri
- Connection string içinde geçen user/password segmentleri

## Kayıt İlkeleri
- **İstek/cevap body loglanmaz** (özellikle password change endpointleri).
- Sadece **whitelist** alanlar loglanır (örn. `jobId`, `targetCount`, `status`).
- Header loglarında `Authorization` ve `Cookie` **asla** yazılmaz.
- Exception loglarında **payload yok**; yalnızca hata kodu ve güvenli özet bulunur.

## Uygulama Notları
- Serilog minimum level üretim için `Information` ve üstü olacak şekilde ayarlı.
- File sink `logs/*.log` altında rolling günlük dosyalarla çalışır.
- EventLog sink sadece Windows ortamında etkinleştirilir.
- Korelasyon için `CorrelationId` (header veya request scope) kullanılacaktır.

## Denetim
- CI aşamasında `dotnet list package --vulnerable` ve log taraması önerilir.
- Prod’da loglar SIEM’e forward edilirken redaction kuralları korunur.