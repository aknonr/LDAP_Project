# Reliability and Error Handling

Bu dokuman; API -> MQ -> Worker -> DB -> SignalR akisinda hata yonetimi, retry ve UI'ya yansitma standartlarini ozetler.

## Temel Prensipler
- DB kaynak-of-truth: UI her zaman REST ile son durumu tekrar okuyabilmeli.
- SignalR best-effort: canli guncelleme icindir, veri kaynagi degildir.
- Password/secret loglanmaz: sadece memory, hata mesajlari redaction'dan gecer.
- Event-driven: komutlar/sonuclar MQ uzerinden akar, polling kullanilmaz.

## Retry ve Timeout
- MassTransit retry: receive endpointlerde default `3` deneme, `5s` interval.
- Retry ayarlari configten degistirilebilir:
  - `Messaging:Consumer:RetryAttempts`
  - `Messaging:Consumer:RetryIntervalSeconds`
- Remote execution timeout:
  - `RemoteExecution:ConnectTimeoutSeconds`
  - `RemoteExecution:OverallTimeoutSeconds`
- Hata kodu standardi:
  - `WINRM_CONNECT_FAILED`, `ACCESS_DENIED`, `TIMEOUT`, `RESOURCE_NOT_FOUND`, `CIRCUIT_OPEN`, `UNKNOWN`

## Dayaniklilik (Outbox + Quorum)
- Quorum queue: command queue'larin HA davranisi (RabbitMQ cluster icin).
- EF Outbox/Inbox:
  - publish ve consume side effect'leri DB transaction ile daha guvenli hale getirir
  - tablolar: `InboxState`, `OutboxMessage`, `OutboxState`
  - config: `Messaging:Outbox:*`

## DLQ (Error Queue) Politikasi
- Consumer retry limiti asildiginda mesajlar **DLQ** olarak kabul ettigimiz `<queue>_error` kuyruguna tasinir (MassTransit default davranis).
- Operasyon:
  - RabbitMQ Management UI'da ilgili `_error` kuyrugunu kontrol et.
  - Hata nedeni duzeltildikten sonra mesajlari manuel olarak requeue/move et (kurum standardina gore).
  - Mesaj iceriginde sifre plain bulunmaz (AES-GCM encrypted payload).

## Kill-Switch (Circuit/Kill)
- Receive endpoint seviyesinde kill-switch aciktir (default):
  - `Messaging:Consumer:KillSwitch:*`
- Kisa surede yuksek oranli **consumer exception** olursa endpoint gecici olarak durdurulur ve `RestartTimeoutSeconds` kadar sonra tekrar dener.
- Not: Remote/LDAP gibi "is hatalari" genelde event ile raporlandigi icin exception olusturmaz; kill-switch daha cok kod bug'u/DB baglanti gibi sistem hatalarini frenler.

## Remote Execution Circuit Breaker (WinRM)
- WinRM/PowerShell cagrilarinda sistematik failure oranı yuksekse circuit-breaker devreye girer ve bir sure **fail-fast** yapar:
  - Config: `RemoteExecution:CircuitBreaker:*`
  - Sonuc: `CIRCUIT_OPEN` error code
- Amaç: WinRM tamamen kapali/ag erisimi yok/kimlik dogrulama sistematik fail gibi durumlarda worker’in agi ve sunuculari "hammer" etmesini engellemek.

## UI'ya Yansitma
- Worker sonucu event publish eder.
- API event consumer:
  - `JobTarget` ve `Job` durumunu gunceller (tracking service)
  - SignalR ile `jobUpdated` ve `targetUpdated` eventlerini broadcast eder
- UI:
  - reconnect + REST ile yeniden senkron
  - `ErrorCode` + `ErrorMessage` + `UpdatedAt` alanlarini temel alir
