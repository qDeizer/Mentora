# Mentora – Teknik Özet

Mentora, psikolog/psikiyatrist randevu yönetimi için rol tabanlý ve konum destekli bir ASP.NET Core MVC uygulamasýdýr.

**Temel Özellikler**
- Rol bazlý kayýt ve giriþ: Doktor ve Hasta rolleri ASP.NET Core Identity ile yönetilir.
- Profil yönetimi: fotoðraf, iletiþim, doðum tarihi, cinsiyet, hakkýnda alaný ve konum kaydý.
- Konum tabanlý arama: profil konumu veya harita üzerinden girilen enlem/boylama göre mesafe filtresi.
- Randevu talep akýþý: hasta talep oluþturur, doktor onay/ret verir; onaylanan randevuda diðer talepler otomatik reddedilir.
- Durum takibi: talepler Beklemede/Onaylandý/Reddedildi; randevular Müsait/Rezerve/Tamamlandý/Gerçekleþmedi.
- Otomatik durum güncelleme: arka plan servisleri süresi geçen randevularý günceller.

**Doktor Özellikleri**
- Uzmanlýk seçimi ve sertifika yükleme.
- Randevu slotu oluþturma: baþlangýç/bitiþ, ücret aralýðý, online/yüz yüze, notlar.
- Gelen talepleri filtreleme ve tek hasta onayý.

**Hasta Özellikleri**
- Filtrelenmiþ arama: doktor, uzmanlýk, fiyat aralýðý, tarih aralýðý, görüþme tipi, mesafe.
- Randevu talebi oluþturma ve iptal.
- Talep durumlarýný izleme.

**Teknik Altyapý**
- Framework: .NET 8 / ASP.NET Core MVC.
- Veritabaný: SQL Server, Entity Framework Core 8 (Code First).
- Konum: NetTopologySuite (SRID 4326) ve mesafe hesaplamasý.
- Kimlik doðrulama: ASP.NET Core Identity (TPH Discriminator: UserType).
- Arka plan: Hosted Service (`AppointmentStatusUpdaterService`).
- Zaman yönetimi: `Turkey Standard Time` / `Europe/Istanbul` dönüþümü ile zaman dilimi uyumu.

**Veri Modeli**
- `Doctor` ve `Patient`, `User` sýnýfýndan türetilir; `UserType` discriminator kullanýlýr.
- Baþlýca iliþkiler: Doctor–Specialty (çoktan çoða, `DoctorSpecialty`), Appointment–Specialty (çoktan çoða, `Appointment_Specialty`), Doctor–Certificate (bire çok), Appointment–Request (bire çok).

**Kurulum Notlarý**
1. `appsettings.Development.json` içindeki `DefaultConnection` deðerini kendi SQL Server örneðinize göre güncelleyin.
2. Harita entegrasyonu için `Views/Account/Register.cshtml` ve `Views/PatientDashboard/Index.cshtml` dosyalarýndaki `[GOOGLE_MAPS_API_KEY]` alanlarýný gerçek anahtar ile deðiþtirin.
3. Uygulama baþlangýcýnda `EnsureCreated` çalýþtýðý için veritabaný otomatik oluþturulur; üretim ortamýnda migrations tercih edilmelidir.

**Ekran Görüntüleri**
- `screenshoots/Ekran görüntüsü 2026-02-07 193639.png`
- `screenshoots/Ekran görüntüsü 2026-02-07 193749.png`
- `screenshoots/Ekran görüntüsü 2026-02-07 193904.png`
- `screenshoots/Ekran görüntüsü 2026-02-07 193942.png`
- `screenshoots/Ekran görüntüsü 2026-02-07 194059.png`
- `screenshoots/Ekran görüntüsü 2026-02-07 194121.png`
- `screenshoots/Ekran görüntüsü 2026-02-07 194139.png`
