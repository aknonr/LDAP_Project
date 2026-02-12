# Realtime Updates (PKG-012)

## Amaç
- Worker sonucu geldiginde API'nin DB durumunu guncelleyip UI'ya canli aktarim yapmasi.

## Mimari
- Hub: `API/Hubs/JobsHub.cs`
- Hub path: `/hubs/jobs`
- Client eventleri:
  - `jobUpdated`
  - `targetUpdated`
- API consumer:
  - `ServerUsageResultEventConsumer`
  - `ServerUpdateResultEventConsumer`
  - `JobProgressEventConsumer`

## Group Modeli
- Client, `SubscribeJob(jobId)` ile `job:{jobId}` grubuna girer.
- Broadcast sadece ilgili gruba gider.

## Windows Kurulum Notlari
- Ayrica SignalR sunucu kurulumu yok; ASP.NET Core runtime icinde gelir.
- IIS host icin:
  - `.NET 8 Hosting Bundle`
  - `WebSocket Protocol` feature acik olmali
- Yalniz Kestrel ile calisacaksa ekstra Windows feature zorunlu degil.

## Paketler
- Zorunlu ek paket yok (`Microsoft.AspNetCore.App` yeterli).
- Opsiyonel scale-out:
  - `Microsoft.AspNetCore.SignalR.StackExchangeRedis`
  - veya managed servis yaklasimi

## Performans Notlari
- `PrefetchCount` ve `ConcurrencyLimit` configten yonetilir.
- 2000+ target icin UI tarafinda paging/virtualization zorunlu.
