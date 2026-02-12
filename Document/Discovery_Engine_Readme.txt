# Discovery Engine (PKG-007)

## Amac
- Sunuculardaki servis hesabi kullanimlarini tespit etmek ve `JobResource` tablosuna yazmak.

## Mimari
- Engine: `Infrastructure/Discovery/DiscoveryEngine.cs`
- Strategy arayuzu: `Application/Abstractions/Discovery/IDiscoveryStrategy.cs`
- Context: `Application/Abstractions/Discovery/DiscoveryContext.cs`

## Mevcut Stratejiler (Stub)
- Service
- ScheduledTask
- IISAppPool
- IISSite
- IISWebApp
- IISVirtualDir
- COMPlus
- UserRight

## Akis
1. Worker discovery command alir.
2. DiscoveryEngine calisir.
3. Kaynaklar `JobResource` olarak yazilir.
4. `ServerUsageResultEvent` publish edilir.

## Not
- Stub stratejiler su an bos liste doner, ileride WinRM/PowerShell ve WMI/CIM ile uygulanacak.
