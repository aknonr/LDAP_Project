# Update Engine (PKG-008)

## Amac
- Discovery ile bulunan kaynaklara yeni sifreyi uygulamak.

## Mimari
- Engine: `Infrastructure/Update/UpdateEngine.cs`
- Strategy arayuzu: `Application/Abstractions/Update/IUpdateStrategy.cs`
- Context: `Application/Abstractions/Update/UpdateContext.cs`

## Mevcut Stratejiler
- Service (idempotent + error-code map aktif)
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
- Service disindaki stratejiler su an stub (UNKNOWN).
- Engine idempotent davranir: `Success` durumundaki resource tekrar islenmez.
