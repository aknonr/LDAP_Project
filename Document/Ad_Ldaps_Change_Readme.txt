# AD LDAPS Password Change (PKG-006)

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Amac
- AD uzerinde `oldPassword + newPassword` ile degistirme yapmak (reset yok).

## Implementasyon
- Service: `Infrastructure/Directory/AdPasswordChangeService.cs`
- Options: `Infrastructure/Directory/LdapOptions.cs`
- Abstraction: `Application/Abstractions/Directory/IAdPasswordChangeService.cs`
- Result modeli: `Application/Models/OperationResult.cs`

## Onemli Notlar
- LDAPS zorunlu: port `636` ve `UseSsl=true`.
- Sertifika dogrulama `ValidateServerCertificate` ile kontrol edilir.
- Password degerleri loglanmaz.
- `UserDnOrUpn` girdisi su formatlardan biri olabilir:
  - DN: `CN=...,OU=...,DC=...`
  - UPN: `user@domain`
  - `DOMAIN\\user` (sAMAccountName)
- Servis DN verilmediyse, RootDSE `defaultNamingContext` uzerinden LDAP search ile DN resolve eder.

## Idempotency (Retry) Notu
- MQ/Worker retry senaryolarinda, AD change daha once basarili olup consumer tekrar calisirsa `oldPassword` INVALID_CREDENTIALS olabilir.
- Bu durumda servis `newPassword` ile bind edebiliyorsa "zaten degismis" kabul edip `Success` doner.

## Error Code Mapping
- `INVALID_CREDENTIALS` -> bind/auth hatasi
- `POLICY_VIOLATION` -> parola politikasi ihlali
- `ACCESS_DENIED` -> yetersiz yetki
- `USER_NOT_FOUND` -> kullanici/DN bulunamadi
- `LDAPS_CONNECT_FAILED` -> baglanti / TLS hatasi
- `TIMEOUT` -> zaman asimi
- `UNKNOWN` -> beklenmeyen hata

## Config (Worker)
```
Ldap:Host
Ldap:Port
Ldap:UseSsl
Ldap:ValidateServerCertificate
Ldap:TimeoutSeconds
Ldap:AuthType
```
