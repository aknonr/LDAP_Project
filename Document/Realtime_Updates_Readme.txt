# Realtime Updates (PKG-012)

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

## Amac
- Worker sonucu geldiginde API DB durumunu gunceller ve UI'ya SignalR ile canli yansitir.

## Mimari
- Hub: `API/Hubs/JobsHub.cs`
- Hub path: config ile gelir: `Realtime:SignalR:HubPath` (default `/hubs/jobs`)
- UI eventleri:
  - `jobUpdated`
  - `targetUpdated`
- Event bridge:
  - Worker publish: `ServerUsageResultEvent`, `ServerUpdateResultEvent`, `JobProgressEvent`
  - API consume + DB update: `ServerUsageResultEventConsumer`, `ServerUpdateResultEventConsumer`, `JobProgressEventConsumer`
  - API broadcast: `JobsHub`

## UI Ayri Deploy
- UI farkli origin ise API tarafinda CORS acilmalidir: `Cors:AllowedOrigins`.
- UI, OIDC ile JWT alir ve:
  - REST cagrisinda `Authorization: Bearer <token>`
  - SignalR baglantisinda token'i gonderebilir (browser WebSocket icin querystring `access_token` yaygindir).
- API tarafinda JWT config'inde hub path icin `access_token` okuma hook'u bulunur.

## Dayaniklilik ve Data Tutarliligi
- DB kaynak-of-truth'dur; SignalR best-effort'tur.
- UI tarafinda:
  - otomatik reconnect (exponential backoff)
  - reconnect sonrasi `GET /jobs/{id}` ve `GET /jobs/{id}/targets` ile tekrar senkron
  - UI state'i `UpdatedAt` ile son yazan kazanir mantigiyla birlestir

## API Scale-Out Notu
- Birden fazla API instance varsa SignalR icin:
  - sticky-session (LB) veya
  - backplane gerekir (or: `Microsoft.AspNetCore.SignalR.StackExchangeRedis`).

## Windows Kurulum Notlari
- Ayrica SignalR sunucu kurulumu yok; ASP.NET Core runtime icinde gelir.
- IIS host icin:
  - `.NET 8 Hosting Bundle`
  - `WebSocket Protocol` feature acik olmali
- Yalniz Kestrel ile calisacaksa ekstra Windows feature zorunlu degil.
