# Inventory API Inventory Sync (PKG-005)

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Amac
- Inventory API envanter bilgisini DB'deki `ServerGroup` ve `ServerInventory` tablolarina senkronlamak.

## Uygulama
- Client: `Infrastructure/InventoryApi/InventoryApiClient.cs`
- Sync service: `Infrastructure/InventoryApi/InventorySyncService.cs`
- Worker job: `Worker/Jobs/InventorySyncJob.cs`

## Sync Kurallari
- Diff rule: `CreatedDate` veya `UpdatedDate` (config ile secilir).
- Yeni kayitlar eklenir, mevcut kayitlar timestamp/alan degisimine gore guncellenir.
- Silme islemi yoktur (soft delete sonraki asama).

## Config Anahtarlari (Worker)
```
InventoryApi:Enabled
InventoryApi:BaseUrl
InventoryApi:InventoryEndpoint
InventoryApi:ApiKey
InventoryApi:ApiKeyHeaderName
InventoryApi:BearerToken
InventoryApi:TimeoutSeconds
InventoryApi:DiffRule
InventoryApi:SyncIntervalSeconds
InventoryApi:InitialDelaySeconds
```

## Guvenlik
- API key/token config placeholder olarak tutulur.
- Loglarda secret yazilmaz.
- TLS varsa BaseUrl HTTPS olmalidir.
