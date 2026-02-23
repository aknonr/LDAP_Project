# Discovery Engine (PKG-007)

## Amac
- Sunuculardaki servis hesabi kullanimlarini tespit etmek ve `JobResource` tablosuna yazmak.

## Mimari
- Engine: `Infrastructure/Discovery/DiscoveryEngine.cs`
- Strategy arayuzu: `Application/Abstractions/Discovery/IDiscoveryStrategy.cs`
- Context: `Application/Abstractions/Discovery/DiscoveryContext.cs`
- Remote execution: `Infrastructure/RemoteExecution/IRemoteCommandExecutor.cs`

## Mevcut Stratejiler
- Service (WinRM/PowerShell ile aktif)
- ScheduledTask (WinRM/PowerShell ile aktif)
- IISAppPool (WinRM/PowerShell ile aktif)
- IISSite (WinRM/PowerShell ile aktif)
- IISWebApp (WinRM/PowerShell ile aktif)
- IISVirtualDir (WinRM/PowerShell ile aktif)
- COMPlus (WinRM/PowerShell ile aktif)
- UserRight (WinRM/PowerShell + secedit export ile aktif)

## Akis
1. Worker discovery command alir.
2. DiscoveryEngine calisir.
3. Kaynaklar `JobResource` olarak yazilir.
4. `ServerUsageResultEvent` publish edilir.

## Not
- Service discovery sonucu `ResourceName=ServiceName`, `ResourcePath=StartName` olarak kaydedilir.
- UserRight discovery sonucu `ResourceName=RightName (or. SeServiceLogonRight)`, `ResourcePath=Account (or. DOMAIN\\user)` olarak kaydedilir.
- UserRight stratejisi `secedit.exe /export /areas USER_RIGHTS` kullandigi icin remote tarafta yeterli yetki (genellikle local admin) gerektirir. Yetki yoksa hedef `ACCESS_DENIED` ile fail olur.
- Remote timeout ayarlari `Worker/appsettings.json` altindaki `RemoteExecution` bolumunden yonetilir.
