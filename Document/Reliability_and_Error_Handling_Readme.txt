# Reliability and Error Handling

Bu dokuman; API -> MQ -> Worker -> DB -> SignalR akisinda hata yonetimi, retry ve UI'ya yansitma standartlarini ozetler.

## Temel Prensipler
- DB kaynak-of-truth: UI her zaman REST ile son durumu tekrar okuyabilmeli.
- SignalR best-effort: canli guncelleme icindir, veri kaynagi degildir.
- Password/secret loglanmaz: sadece memory, hata mesajlari redaction'dan gecer.
- Event-driven: komutlar/sonuclar MQ uzerinden akar, polling kullanilmaz.

## Retry ve Timeout
- MassTransit retry: receive endpointlerde default `3` deneme, `5s` interval.
- Remote execution timeout:
  - `RemoteExecution:ConnectTimeoutSeconds`
  - `RemoteExecution:OverallTimeoutSeconds`
- Hata kodu standardi:
  - `WINRM_CONNECT_FAILED`, `ACCESS_DENIED`, `TIMEOUT`, `RESOURCE_NOT_FOUND`, `UNKNOWN`

## Dayaniklilik (Outbox + Quorum)
- Quorum queue: command queue'larin HA davranisi (RabbitMQ cluster icin).
- EF Outbox/Inbox:
  - publish ve consume side effect'leri DB transaction ile daha guvenli hale getirir
  - tablolar: `InboxState`, `OutboxMessage`, `OutboxState`
  - config: `Messaging:Outbox:*`

## UI'ya Yansitma
- Worker sonucu event publish eder.
- API event consumer:
  - `JobTarget` ve `Job` durumunu gunceller (tracking service)
  - SignalR ile `jobUpdated` ve `targetUpdated` eventlerini broadcast eder
- UI:
  - reconnect + REST ile yeniden senkron
  - `ErrorCode` + `ErrorMessage` + `UpdatedAt` alanlarini temel alir
