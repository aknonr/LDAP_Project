# Migrations

Bu klasör, EF Core migrations dosyalarının bulunacağı yerdir.

Oluşturmak için:
1. `ConnectionStrings:Default` değerini gerçek SQL Server bağlantı dizesiyle ayarla.
2. Aşağıdaki komutu çalıştır:
   - `dotnet ef migrations add InitialCreate --project Infrastructure --startup-project API`

> Not: Bu aşamada sadece iskelet hazırlandı; migration dosyaları henüz oluşturulmadı.
