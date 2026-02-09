# Config ve Secrets Stratejisi

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

## Secret Kaynağı Akışı (Özet)
1. Uygulama başlarken `KeySource` okunur.
2. Seçilen store’dan (Credential Manager / Vault) `KeyName` ile anahtar çekilir.
3. AES‑GCM ile **sadece** bellek içinde şifreleme yapılır.
4. Log / DB / MQ içinde plain şifre tutulmaz.

## Notlar
- Connection string ve payload anahtarı **CI/CD secret store** üzerinden verilir.
- Prod ortamda `RequireHttpsMetadata` ve TLS doğrulama zorunludur.
- `appsettings.json` dosyaları **source control**’a girebilir; secret içermez.
