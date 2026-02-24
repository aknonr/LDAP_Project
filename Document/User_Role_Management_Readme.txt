# User/Role Management (PKG-019)

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Amac
- Uygulama kullanicilarini DB allowlist uzerinden yonetmek (AppUsers).
- Rol atama/geri alma (AppUserRoles + Roles).
- Soft delete: kullanici silinmez, `IsActive=false` yapilir.

## Guvenlik Kurallari
- Default/Fallback policy: authenticated + group allowlist + DB allowlist (fail-closed).
- Admin API endpointleri: `Admin,SuperAdmin`.
- `SuperAdmin` rolunu atamak/degistirmek icin cagirani `SuperAdmin` olmalidir (API kontrolu).
- Lockout onlemi:
  - Son aktif admin/superadmin kullanicisi pasif edilemez.
  - Son aktif admin/superadmin kullanicisindan Admin/SuperAdmin rolleri kaldirilamaz.

## API Endpointleri
- `GET /admin/users?skip=0&take=100`
- `POST /admin/users` (upsert)
- `PUT /admin/users/{id}/active`
- `PUT /admin/users/{id}/roles`
- `GET /admin/roles`

## Bootstrap (Ilk Kurulum)
DB allowlist + DB role mapping fail-closed oldugu icin ilk kurulumda en az 1 admin gerekir.

Iki opsiyon:
1. `Auth:Bootstrap` (API startup seeding):
   - `Enabled=true` ve `InitialAdminSubject` verilirse, ilk aktif admin yoksa 1 adet `SuperAdmin` olusturur/aktiflestirir.
   - Rolleri (Admin/Operator/Viewer/SuperAdmin) eksikse seed eder.
   - Ilk kurulumdan sonra `Enabled=false` yapilmasi onerilir.
2. Manuel SQL:
   - AppUsers + Roles + AppUserRoles tablolarina 1 admin kullanici eklenir.

## Audit
- Admin islemleri `AuditLogs` tablosuna yazilir.
- Audit summary sanitize edilir; password/secrets yazilmaz.

