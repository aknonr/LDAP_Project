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
  - `OperationFailureException` (error code propagation)
- Verify abstraction:
  - `IVerifyEngine`
  - `VerifyContext`, `VerifyResult`

## Mimari Not
- API katmani sadece abstraction'lara baglidir.
- Infrastructure katmani bu abstraction'larin implementasyonunu saglar.
- DTO ayirma katmani kullanici talebiyle sonraki/gec asamaya ertelendi.

## Sonraki Plan
- Password-change job orkestrasyonu: AD change + update + verify
- Yetki/rol modelini (SuperAdmin rapor) genisletme
