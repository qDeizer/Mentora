# Mentora

Mentora, psikolog/psikiyatrist randevu yönetimi icin rol tabanli ve konum destekli bir ASP.NET Core MVC uygulamasidir. Hasta ve doktor kullanicilari icin randevu arama, talep ve onay akislari; sistem tarafinda ise otomatik durum guncellemeleri saglar.

**One Cikanlar**
- Doktor ve Hasta rolleri ile kayit/giris (ASP.NET Core Identity).
- Konum tabanli arama ve mesafe filtreleme (NetTopologySuite, SRID 4326).
- Randevu talep/onay mekanizmasi ve cakisma engelleme.
- Arka planda calisan servis ile otomatik durum guncelleme.

**Hasta Ozellikleri**
- Uzmanlik, fiyat araligi, tarih araligi, gorusme tipi ve mesafe filtreleri ile arama.
- Randevu talebi olusturma ve durum takibi.
- Talep iptali.

**Doktor Ozellikleri**
- Uzmanlik secimi ve sertifika yukleme.
- Randevu slotu olusturma (baslangic/bitis, ucret araligi, online/yuz yuze, notlar).
- Gelen talepleri inceleme ve tek hasta onayi.

**Otomatik Surecler**
- Sureyi gecen randevularin "Tamamlandi" veya "Gerceklesmedi" durumuna alinmasi.
- Bir randevuya onay verildiginde diger taleplerin otomatik reddi.

**Teknik Altyapi**
- .NET 8 / ASP.NET Core MVC
- SQL Server + Entity Framework Core 8 (Code First)
- NetTopologySuite ile coğrafi veri ve mesafe hesaplama
- Background Hosted Service: `AppointmentStatusUpdaterService`
- Identity TPH Discriminator: `UserType`

**Kurulum**
1. .NET 8 SDK ve SQL Server kurun (LocalDB/Express yeterlidir).
2. Ornek ayarlari kopyalayin:

```powershell
Copy-Item appsettings.Development.sample.json appsettings.Development.json
```

3. `appsettings.Development.json` icindeki `DefaultConnection` degerini kendi SQL Server orneginize gore guncelleyin.
4. Google Maps entegrasyonu icin asagidaki dosyalarda `[GOOGLE_MAPS_API_KEY]` yerini gercek anahtarla degistirin.

- `Views/Account/Register.cshtml`
- `Views/PatientDashboard/Index.cshtml`

5. Uygulamayi calistirin:

```powershell
dotnet restore
dotnet run
```

Not: Uygulama baslangicinda `EnsureCreated` calisir. Uretimde migrations tercih edilmelidir.

**Repo Notlari**
- `wwwroot/images/profiles` ve `wwwroot/images/certificates` dizinleri kullanici yuklemeleri icindir. Bu nedenle repo disinda tutulur ve bos `.gitkeep` dosyalariyla izlenir.

**Ekran Goruntuleri**
![Ana Sayfa](screenshoots/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202026-02-07%20193904.png)
![Doktor Dashboard](screenshoots/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202026-02-07%20193942.png)
![Randevu Ara](screenshoots/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202026-02-07%20194059.png)
![Harita Gorunumu](screenshoots/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202026-02-07%20194139.png)
