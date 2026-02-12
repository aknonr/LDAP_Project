# TH API Inventory Sync (PKG-005)

## Amac
- TH API envanter bilgisini DB'deki `ServerGroup` ve `ServerInventory` tablolarina senkronlamak.

## Uygulama
- Client: `Infrastructure/ThApi/ThApiClient.cs`
- Sync service: `Infrastructure/ThApi/InventorySyncService.cs`
- Worker job: `Worker/Jobs/InventorySyncJob.cs`

## Sync Kurallari
- Diff rule: `CreatedDate` veya `UpdatedDate` (config ile secilir).
- Yeni kayitlar eklenir, mevcut kayitlar timestamp/alan degisimine gore guncellenir.
- Silme islemi yoktur (soft delete sonraki asama).

## Config Anahtarlari (Worker)
```
ThApi:Enabled
ThApi:BaseUrl
ThApi:InventoryEndpoint
ThApi:ApiKey
ThApi:ApiKeyHeaderName
ThApi:BearerToken
ThApi:TimeoutSeconds
ThApi:DiffRule
ThApi:SyncIntervalSeconds
ThApi:InitialDelaySeconds
```

## Guvenlik
- API key/token config placeholder olarak tutulur.
- Loglarda secret yazilmaz.
- TLS varsa BaseUrl HTTPS olmalidir.
