# Verify Flow (PKG-010/011/012)

## Amac
- Update sonrasi kaynaklarin hedef hesapla eslesmesini dogrulamak ve final hedef sonucunu uretmek.

## Mimari
- Worker consumer: `Worker/Consumers/VerifyServerConsumer.cs`
- Engine: `Infrastructure/Verify/VerifyEngine.cs`
- Abstraction: `Application/Abstractions/Verify/IVerifyEngine.cs`
- Veri kaynagi: mevcut `JobTarget` + `JobResource` kayitlari ve sunucudan tekrar yapilan discovery

## Akis
1. Update basarili olunca Worker (opsiyonel) `VerifyServerCommand` gonderir.
2. Verify consumer payload decrypt yapar (plain sifre sadece memory).
3. VerifyEngine, hedefteki kaynak tiplerine gore discovery stratejilerini tekrar calistirir.
4. Her kaynak icin:
   - Kaynak bulunamazsa: `RESOURCE_NOT_FOUND`
   - Kaynak kimligi (identity) hedef hesapla eslesmiyorsa: `VERIFY_MISMATCH`
   - Eslesiyorsa: `Success`
5. Verify sonucu `ServerUpdateResultEvent` ile publish edilir.
6. API event consumer DB'de target/job durumunu gunceller ve SignalR ile UI'ya yansitir.

## ErrorCode Standartlari
- `VERIFY_TARGET_ACCOUNT_REQUIRED`: target account bos/eksik
- `RESOURCE_NOT_FOUND`: kaynak veya strategy bulunamadi
- `ACCESS_DENIED`: WinRM/remote erisim reddedildi
- `TIMEOUT`: remote komut timeout
- `WINRM_CONNECT_FAILED`: WinRM baglanti sorunu
- `VERIFY_MISMATCH`: identity hedef hesapla eslesmedi
- `UNKNOWN`: diger hatalar

## Notlar
- Verify su an kimlik/assignment kontrolu yapar; "sifrenin gercekten degistigi" (AD bind/oturum acma) kontrolu ayri bir asamadir.
- UI icin canli guncelleme SignalR ile gelir; SignalR kesilirse UI REST endpointlerinden durumu tekrar okuyabilir.
