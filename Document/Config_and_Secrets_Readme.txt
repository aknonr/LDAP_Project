# Config ve Secrets Stratejisi

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

Bu doküman PKG-002 kapsamındadır ve ortam bazlı konfigürasyon + secret yönetimi için iskeleti tanımlar. **Şifreler, tokenlar ve anahtarlar config dosyalarında plain tutulmaz.**

## Temel İlkeler
- **Config ayrı, secret ayrı:** `appsettings*.json` içinde sadece placeholder bulunur.
- **Secret kaynakları:** Windows Credential Manager veya kurum standardı Vault.
- **Loglarda secret yok:** `oldPassword`, `newPassword`, `tokens`, `secrets` gibi alanlar loglanmaz.
- **MQ payload korunur:** AES‑GCM ile şifrelenmiş payload, `KeyId` ile işaretlenir.

## Ortam Bazlı Yapı
`appsettings.json` (genel) + `appsettings.{Environment}.json` (override) kullanılır. Örnek:
- `appsettings.json`: ortak ayarlar ve placeholder’lar
- `appsettings.Development.json`: sadece seviye/override (örn. log seviyesi)

## Zorunlu Placeholder’lar
Bu alanlarda gerçek secret **asla** yer almaz:
- `ConnectionStrings:Default` → `<SQL_SERVER_CONNECTION_STRING>`
- `Security:PayloadEncryption:KeyId` → `<KEY_ID>`
- `Security:PayloadEncryption:KeyName` → `<KEY_NAME>`
- `Security:PayloadEncryption:KeySource` → `WindowsCredentialManager|Vault`
- `ThApi:ApiKey` → `<TH_API_KEY>`
- `ThApi:BearerToken` → `<TH_API_BEARER>`

## Secret Kaynağı Akışı (Özet)
1. Uygulama başlarken `KeySource` okunur.
2. Seçilen store’dan (Credential Manager / Vault) `KeyName` ile anahtar çekilir.
3. AES‑GCM ile **sadece** bellek içinde şifreleme yapılır.
4. Log / DB / MQ içinde plain şifre tutulmaz.

## Notlar
- Connection string ve payload anahtarı **CI/CD secret store** üzerinden verilir.
- Prod ortamda `RequireHttpsMetadata` ve TLS doğrulama zorunludur.
- `appsettings.json` dosyaları **source control**’a girebilir; secret içermez.
- TH API anahtari secret store’dan cekilmelidir.
