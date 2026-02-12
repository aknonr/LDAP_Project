# Migrations

Bu klasor EF Core migration dosyalarini barindirir.

## Mevcut Migration
- `AddServerInventoryExternalId` (ilk kurulum + ServerInventories `ExternalId` ve index)
- `AddMassTransitOutbox` (MassTransit EF outbox/inbox tabloları: `InboxState`, `OutboxMessage`, `OutboxState`)

## Yeni Migration Olusturma
1. (Opsiyonel) Tooling icin gercek connection string vermek istersen:
   - `ADPM_CONNECTIONSTRING` environment variable ayarla.
2. Komutu calistir:
   - `dotnet ef migrations add <Name> --project Infrastructure/Infrastructure.csproj --context Infrastructure.Persistence.AdpmDbContext --output-dir Migrations`

## Not
- `Infrastructure/Persistence/AdpmDbContextFactory.cs` tooling icin design-time context saglar.
