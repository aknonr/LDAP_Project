# EnterpriseADPasswordManager (LDAP_Project)

## State Snapshot (2026-02-23)
- Snapshot date: `2026-02-23`
- Current phase: `PKG-019 (in_progress_partial)`
- Last stable completion: `PKG-018`
- Next targets: `PKG-019 close -> PKG-014 -> PKG-020 -> PKG-021 -> PKG-022 -> PKG-023`

Bu repo, kurumsal AD sifre degistirme ve servis hesabi kullanimini yonetmek icin `Onion Architecture + Worker Services + Event Driven` yaklasimiyla ilerler.

## Hedef Stack
- .NET 8
- SQL Server 2022
- RabbitMQ + MassTransit
- SignalR

## Katmanlar
- `Domain`
- `Application`
- `Infrastructure`
- `API`
- `Worker`
- `Document`

## Guncel Dokuman Giris Noktasi
- `Document/Project_General_Readme.txt`

## Roadmap
- `Document/roadmap.json`
