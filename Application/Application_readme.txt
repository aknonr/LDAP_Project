# Application Katmani

## Amac
- Use-case orkestrasyonu, is kurallari ve abstraction sozlesmeleri burada tutulur.

## Mevcut Durum
- Job use-case'leri:
  - `CreateDiscoveryJobUseCase`
  - `CreatePasswordChangeJobUseCase`
  - `GetJobStatusUseCase`
  - `GetJobTargetsUseCase`
- Admin use-case'leri:
  - `ListUsersUseCase`
  - `UpsertUserUseCase`
  - `SetUserActiveUseCase`
  - `SetUserRolesUseCase`
  - `ListRolesUseCase`
- Messaging contract'lari:
  - Command: `DiscoverServerUsageCommand`, `UpdateServerResourcesCommand`, `VerifyServerCommand`
  - Event: `ServerUsageResultEvent`, `ServerUpdateResultEvent`, `JobProgressEvent`
- Repository abstraction:
  - `IJobRepository`, `IServerGroupRepository`, `IIdentityRepository`
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
- PKG-019: User/Role yonetimi: soft delete + audit raporlama (tamamlama)
- PKG-020: Permission modeli (fine-grained authorization)
