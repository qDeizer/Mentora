# Mentora kod rehberi

Bu dosya okul demosunda hangi kodun nerede çalıştığını hızlı bulmak için yazıldı.

## Giriş, kullanıcı ve profil

- `Controllers/AccountController.cs`
  - Kayıt, giriş, e-posta doğrulama, profil güncelleme, sertifika ekleme/silme işlemleri burada.
- `ViewModels/RegisterViewModel.cs`, `LoginViewModel.cs`, `ProfileEditViewModel.cs`
  - Form alanları ve kullanıcıya dönen validasyon mesajları burada.
- `Views/Account/*`
  - Giriş, kayıt, profil, şifre sıfırlama ve doğrulama ekranları.

## Tema ve görünüm

- `Models/User.cs`
  - `ThemePreference` ve `LayoutDensity` kullanıcı tercihini tutar.
- `Controllers/SettingsController.cs`
  - `/Settings/Appearance` endpointi tema ve yoğunluk tercihini kaydeder.
- `Views/Shared/_Layout.cshtml`
  - Tema menüsü ve sayfa yüklenmeden önce tema uygulayan küçük script burada.
- `wwwroot/js/site.js`
  - Tema değişimini yakalar, localStorage'a yazar ve backend'e gönderir.
- `wwwroot/css/site.css`
  - Koyu tema ve kompakt görünüm stilleri burada.

## Randevu oluşturma ve listeleme

- `Controllers/AppointmentController.cs`
  - Doktorun yeni randevu/özel teklif oluşturması ve randevu silmesi.
- `Services/AppointmentService.cs`
  - Randevu oluşturma kuralları, çevrim içi link doğrulaması, hasta/doktor randevu listeleri.
- `ViewModels/CreateAppointmentViewModel.cs`, `AppointmentViewModel.cs`
  - Randevu formu ve kartlarda kullanılan alanlar.
- `Views/Shared/_CreateAppointmentPartial.cshtml`
  - Doktor panelindeki randevu oluşturma formu.
- `Views/DoctorDashboard/Index.cshtml`
  - Doktorun randevu kartları.
- `Views/DoctorDashboard/Scheduler.cshtml`
  - Haftalık/aylık çizelge görünümü.

## Hasta randevu arama ve talepler

- `Controllers/PatientDashboardController.cs`
  - Hasta randevu arama, kesinleşmiş randevular, puanlama ve takvim dosyası.
- `Views/PatientDashboard/Index.cshtml`
  - Hasta randevu arama ekranı.
- `Views/PatientDashboard/Appointments.cshtml`
  - Hastanın yaklaşan/geçmiş randevuları ve değerlendirme formu.
- `wwwroot/js/patient-dashboard.js`
  - Hasta arama filtresi, harita popup'ları, talep modalı ve otomatik form submitleri.

## Talep onay/red akışı

- `Controllers/AppointmentRequestController.cs`
  - Hastanın randevu talebi oluşturması.
- `Controllers/RequestController.cs`
  - Doktorun talep onay/red işlemleri ve hastanın talep iptali.
- `Services/AppointmentRequestService.cs`
  - Talep oluşturma, onaylama, otomatik red, çakışan slot kapatma, mail ve bildirim üretimi.
- `ViewModels/AppointmentRequestViewModel.cs`
  - Doktor/hasta talep listesi, onay modalı önizleme verileri ve ön bilgi alanları.
- `Views/Request/DoctorRequests.cshtml`
  - Doktorun gelen talepleri, onay etki modalı ve otomatik red mesajı.
- `Views/Request/PatientRequests.cshtml`
  - Hastanın kendi talepleri.

## Klinik notlar ve kişiler

- `Services/ClinicalNoteService.cs`
  - Not oluşturma, görünürlük, paylaşım, yorum, kilit ve çoklu işlem kuralları.
- `Controllers/ClinicalNotesController.cs`
  - Doktor ve hasta not ekranlarının ana controller'ı.
- `Views/ClinicalNotes/Index.cshtml`
  - Doktorun klinik not ekranı.
- `Views/ClinicalNotes/MyNotes.cshtml`
  - Hastanın kendisine ait not ekranı.
- `Services/PeopleService.cs`, `Controllers/PeopleController.cs`
  - Doktor-hasta ilişki listesi, profil yetkileri ve ilişki kesme akışı.

## Demo veri

- `Data/ApplicationDbSeeder.cs`
  - Demo doktor/hasta, randevu, talep, özel teklif, klinik not ve rutin verileri burada kod üzerinden üretilir.
- `appsettings.json`
  - `Seed:DemoData` açık. Demo şifreleri kodda `asdasd`.

## Veritabanı değişiklikleri

- `Models/Appointment.cs`
  - `MeetingLink` alanı eklendi.
- `Models/AppointmentRequest.cs`
  - Talep ön bilgi alanları eklendi.
- `Models/User.cs`
  - Tema ve görünüm tercihleri eklendi.
- `Data/Migrations/20260618090000_SchoolDemoThemeAndRequests.cs`
  - Yeni alanların migration dosyası.
- `Data/Migrations/ApplicationDbContextModelSnapshot.cs`
  - EF model snapshot güncellemesi.

## Testte gezilecek kısa demo akışı

1. Hasta hesabıyla randevu ara, başvuru sebebi/aciliyet/beklenti doldurup talep gönder.
2. Doktor hesabıyla gelen talebi aç, onay modalında diğer başvuranları ve çakışan slotları kontrol et.
3. Onayla; diğer bekleyen taleplerin otomatik reddedildiğini kontrol et.
4. Hasta hesabıyla `Randevularım` ekranında çevrim içi linki ve takvim indirmeyi kontrol et.
5. Geçmiş tamamlanmış randevuya puan/yorum ver.
6. Tema menüsünden açık/koyu ve rahat/kompakt görünümü değiştir.
