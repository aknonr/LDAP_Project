# Update Engine (PKG-008)

## Amac
- Discovery ile bulunan kaynaklara yeni sifreyi uygulamak.

## Mimari
- Engine: `Infrastructure/Update/UpdateEngine.cs`
- Strategy arayuzu: `Application/Abstractions/Update/IUpdateStrategy.cs`
- Context: `Application/Abstractions/Update/UpdateContext.cs`

## Mevcut Stratejiler
- Service (WinRM/CIM ile aktif, idempotent + error-code map)
- ScheduledTask (WinRM/PowerShell ile aktif)
- IISAppPool (WinRM/PowerShell + WebAdministration ile aktif)
- IISSite (WinRM/PowerShell + WebAdministration ile aktif)
- IISWebApp (WinRM/PowerShell + WebAdministration ile aktif)
- IISVirtualDir (WinRM/PowerShell + WebAdministration ile aktif)
- COMPlus (WinRM/PowerShell + COMAdmin ile aktif)

## Akis
1. Worker update command alir.
2. Payload decrypt edilir (plain sifre sadece memory).
2.1. Target icin `JobResource` yoksa Worker, discovery stratejileri ile (WinRM/PowerShell) kaynaklari bulur ve **hedef account** ile eslesenleri `JobResource` olarak olusturur.
3. UpdateEngine kaynaklari gunceller.
4. Verify zinciri aciksa (`Verification:EnablePostUpdateVerification=true`) verify komutu kuyruga gonderilir ve hedef `InProgress` tutulur.
5. `ServerUpdateResultEvent` publish edilir (UI canli guncelleme icin).

## Not
- Engine idempotent davranir: `Success` durumundaki resource tekrar islenmez.
- Script parametreleri base64 ile enjekte edilir; plain sifre log/DB/MQ'ya yazilmaz.
- Target account kullanimini bulamazsa update `no-op` kabul edilir ve hedef `Success` ile tamamlanir.
