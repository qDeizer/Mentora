# Mentora

Mentora, psikolog/hasta randevu yonetimi icin ASP.NET Core MVC (.NET 8) tabanli bir uygulamadir.
Bu surumde proje deploy-ready hardening seviyesine yaklastirildi.

## Yeni Eklenenler (vNext)

- Cakisma yonetimi sertlestirildi:
  - Onaylanan talep ile cakisabilecek `Available` slotlar fiziksel silinmiyor.
  - Slotlar `CancelledByConflict` statusu ile kapatiliyor.
  - Bu slotlara bagli bekleyen talepler otomatik `Rejected` oluyor.
  - Islem transaction icinde idempotent sekilde calisiyor.
- E-posta altyapisi DB tabanli outbox'a tasindi:
  - `EmailOutboxMessages` tablosu
  - Retry + exponential backoff
  - SMTP test e-postasi endpointi
- Dosya yukleme guvenligi:
  - MIME + magic-byte dogrulamasi
  - Profil fotografinda tur/limit kontrolu ve 512x512 kare normalize
  - Sertifikalarda whitelist (jpg/png/webp/pdf) ve boyut limiti
- Guvenlik/operasyon:
  - Rate limiting (auth ve write endpointleri)
  - Guvenlik header'lari
  - Health checks: `/health/live`, `/health/ready`
- Klinik notlar:
  - Hasta not paylasimi geri alma (`RevokeShare`)
  - Paylasim denetim izi (kimle ne zaman paylasildi/geri alindi)
  - Gorunurluk kurallari servis katmaninda aktif paylasimla sinirlandi
- Otomasyon:
  - Duraklatmada gun bazli preset + tarihe kadar durdurma
  - Uretilemeyen slotlar icin neden bazli loglar
- Deploy temeli:
  - `Dockerfile`, `docker-compose.yml`, `.env.example`, `appsettings.Production.sample.json`
- Hesap guvenligi temelleri:
  - Profil guncelleme ekrani (kisisel bilgi + konum + profil fotografi)
  - E-posta dogrulama maili gonderimi ve yeniden gonderim
  - Sifremi unuttum / sifre sifirlama mail akisi
  - Login ekraninda yeni auth akislari

## Teknoloji

- .NET 8 / ASP.NET Core MVC
- Entity Framework Core 8 (Code First + Migrations)
- SQL Server (LocalDB varsayilan)
- ASP.NET Identity
- NetTopologySuite (geography)
- MailKit (SMTP)
- SixLabors.ImageSharp (profil fotograflari icin normalize)

## Hizli Baslatma (2 Mod)

Bu projeyi iki farkli sekilde calistirabilirsiniz:

- `Docker modu` (karsi tarafa gondermek icin en kolay)
- `Local modu` (gelistirme yaparken hizli)

### 1) Docker modu (teknik bilmeyen kullanici icin)

On kosul:
- Sadece Docker Desktop kurulu ve acik olmali.

Adim:
1. Proje klasorunu acin.
2. `start-mentora-docker.bat` dosyasina cift tiklayin.
3. Tarayici otomatik `http://localhost:8080` adresinde acilir.

Kapatma:
- `stop-mentora-docker.bat` dosyasina cift tiklayin.

Not:
- Ilk acilista image build daha uzun surebilir.
- `.env` yoksa script otomatik `.env.example` dosyasindan olusturur.

### 2) Local modu (gelistirme)

`start-mentora.bat` veya `start-mentora-local.bat` calistirin:

```bat
start-mentora.bat
```

Manuel alternatif:

```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project PsikologProje_Void.csproj
```

- Local app: `http://localhost:5000`
- Docker app: `http://localhost:8080`
- Docker SQL: `localhost,14333`

## Ornek Hesaplar

Demo hesaplar sadece demo seed aciksa olusur (`Seed:DemoData` / `SEED_DEMO_DATA=true`):

- Doktor 1: `demo.doctor@mentora.local` / `Mentora123!`
- Doktor 2: `demo.doctor2@mentora.local` / `Mentora123!`
- Hasta: `demo.patient@mentora.local` / `Mentora123!`

## SMTP Ayari

`appsettings.json` veya environment variable ile:

```json
"Smtp": {
  "Host": "smtp.yourprovider.com",
  "Port": 587,
  "UseSsl": true,
  "UserName": "smtp-user",
  "Password": "smtp-password",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "Mentora"
}
```

Bildirim ayarlari ekranindan test maili kuyruga atabilirsiniz.

## Detayli Loglama

Proje Serilog ile hem text hem JSON log uretir:

- Text log: `logs/mentora-YYYYMMDD.log`
- JSON log: `logs/mentora-YYYYMMDD.ndjson`

`appsettings*.json` icindeki `DetailedLogging` ayarlarindan request/response body loglama, EF detay seviyesi, hassas alan maskeleme ve skip path listesi yonetilir.

## Google Maps Ayari

```json
"GoogleMaps": {
  "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
}
```

- Register, hasta randevu kesfetme ve profil konum guncelleme ekranlari bu ayari kullanir.
- API key tanimli degilse harita UI'si guvenli fallback ile devre disi kalir, ancak Google Maps yol tarifi linkleri calismaya devam eder.

## Upload Politikalari

```json
"UploadPolicy": {
  "ProfilePhotoMaxBytes": 2097152,
  "ProfilePhotoSizePx": 512,
  "CertificateMaxBytes": 5242880
}
```

## Yeni Endpointler

- `GET /health/live`
- `GET /health/ready`
- `POST /Settings/Notifications/TestEmail`
- `POST /ClinicalNotes/RevokeShare`

## Frontend Redesign Dokumani

Stitch icin guncel dokuman:

- `docs/frontend-redesign-brief.md`
