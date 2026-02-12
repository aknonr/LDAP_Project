# Application Katmani

## Amac
- Use-case orkestrasyonu, is kurallari ve abstraction sozlesmeleri burada tutulur.

## Mevcut Durum
- Job use-case'leri:
  - `CreateDiscoveryJobUseCase`
  - `CreatePasswordChangeJobUseCase`
  - `GetJobStatusUseCase`
  - `GetJobTargetsUseCase`
- Messaging contract'lari:
  - Command: `DiscoverServerUsageCommand`, `UpdateServerResourcesCommand`, `VerifyServerCommand`
  - Event: `ServerUsageResultEvent`, `ServerUpdateResultEvent`, `JobProgressEvent`
- Security abstraction:
  - `IPayloadProtector`
- Tracking abstraction:
  - `IJobTrackingService`
  - `TargetUpdateSnapshot`, `JobProgressSnapshot`
- Audit abstraction:
  - `IAuditTrailWriter`
  - `IAuditTrailReader`
- Inventory abstraction:
  - `IThApiClient`
  - `IInventorySyncService`
  - `ThInventoryRecord`, `InventorySyncSummary`
- Directory abstraction:
  - `IAdPasswordChangeService`
  - `AdPasswordChangeRequest`
  - `OperationResult`
- Discovery/Update abstraction:
  - `IDiscoveryEngine`, `IDiscoveryStrategy`
  - `IUpdateEngine`, `IUpdateStrategy`
  - `DiscoveryContext`, `DiscoveryResult`
  - `UpdateContext`, `UpdateResult`

## Mimari Not
- API katmani sadece abstraction'lara baglidir.
- Infrastructure katmani bu abstraction'larin implementasyonunu saglar.
- DTO ayirma katmani kullanici talebiyle sonraki/gec asamaya ertelendi.

## Sonraki Plan
- PKG-006: LDAPS password change use-case servis akisi
- PKG-007/008: Discovery ve update strategy abstraction'larini genisletme
