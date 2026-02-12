# Update Engine (PKG-008)

## Amac
- Discovery ile bulunan kaynaklara yeni sifreyi uygulamak.

## Mimari
- Engine: `Infrastructure/Update/UpdateEngine.cs`
- Strategy arayuzu: `Application/Abstractions/Update/IUpdateStrategy.cs`
- Context: `Application/Abstractions/Update/UpdateContext.cs`

## Mevcut Stratejiler (Stub)
- Service
- ScheduledTask
- IISAppPool
- IISSite
- IISWebApp
- IISVirtualDir
- COMPlus

## Akis
1. Worker update command alir.
2. Payload decrypt edilir (plain sifre sadece memory).
3. UpdateEngine kaynaklari gunceller.
4. `ServerUpdateResultEvent` publish edilir.

## Not
- Stratejiler su an stub (UNKNOWN). Gercek WinRM/PowerShell implementasyonu sonraki adim.
