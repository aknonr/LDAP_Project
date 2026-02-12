# Migrations

Bu klasor EF Core migration dosyalarini barindirir.

## Mevcut Migration
- `AddServerInventoryExternalId` (ServerInventories tablosuna `ExternalId` ve index)

## Yeni Migration Olusturma
1. `ConnectionStrings:Default` degerini gercek SQL Server baglanti dizesiyle ayarla.
2. Komutu calistir:
   - `dotnet ef migrations add <Name> --project Infrastructure --startup-project Infrastructure`

## Not
- `Infrastructure/Persistence/AdpmDbContextFactory.cs` tooling icin design-time context saglar.
