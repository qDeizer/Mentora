# Mentora çalışma günlüğü

## 2026-06-18 1. etap - Keşif ve plan

Yapılanlar:
- Ekli metinler okundu ve tekrar eden dosyalar ayıklandı.
- Proje ASP.NET Core MVC, EF Core, SQL Server, Identity ve Docker Compose yapısı olarak incelendi.
- Mevcut özelliklerin çoğunun daha önce kısmen eklendiği görüldü: randevu, talep, not, bildirim, outbox, sağlık kontrolü, Docker dosyaları.
- Yerel ortam kontrol edildi. `dotnet` PATH içinde bulunamadı. Docker Desktop binary var ama Docker daemon kararsız cevap verdi.
- Geliştirme stratejisi belirlendi: önce kodu lokal dosyalarda düzenle, Docker Desktop toparlanmazsa doğrulama/deploy aşamasını droplet üzerinde yap.

Hatalar / riskler:
- Yerel `dotnet build` şu an çalıştırılamıyor.
- Docker Desktop API 500 hatası verdiği için lokal compose güvenilir değil.
- Git çalışma ağacında kullanıcıya ait olabilecek silinmiş ekran görüntüleri ve untracked snapshot var; bunlara dokunulmayacak.

Sonraki adım:
- Demo seed, talep ön bilgi formu, doktor onay popup önizlemesi ve işlem özeti eklenecek.

## 2026-06-18 2. etap - İşlev ekleme ve Türkçeleştirme

Yapılanlar:
- Demo seed genişletildi: 2 doktor, 2 hasta, ortak şifre `asdasd`, bekleyen/çakışan talepler, geçmiş randevu, çevrim içi linkli randevu, özel teklif ve klinik not örnekleri eklendi.
- Randevu talebine başvuru sebebi, önceki destek, aciliyet ve beklenti alanları eklendi.
- Doktor gelen talepler ekranındaki onay modalı genişletildi. Modal artık aynı slota başvuran diğer hastaları, çakışan slotları, tahmini mail/bildirim etkisini ve düzenlenebilir otomatik red mesajını gösteriyor.
- Talep onaylandığında aynı randevudaki diğer bekleyen talepler ve çakışan slot talepleri otomatik reddedilecek şekilde servis akışı güncellendi.
- Hastaya “Randevularım” ekranı eklendi. Yaklaşan/geçmiş randevular, çevrim içi görüşme linki, rota linki, takvim dosyası ve geçmiş tamamlanmış randevu değerlendirmesi eklendi.
- Tema ve görünüm yoğunluğu ayarı eklendi. Tercih hem tarayıcıda hem kullanıcı profilinde saklanıyor.
- Frontendde görünen İngilizce ve Türkçe harfsiz metinler büyük ölçüde temizlendi. “Online” görünen metinleri “Çevrim içi” standardına çekildi.
- Hasta harita kartlarında kapanma/yeniden açılma ve bilgi penceresi bağlama hataları düzeltildi.

Hatalar / çözümler:
- Doktor talep listesindeki sıralama butonu `submitWithScroll()` adlı olmayan fonksiyonu çağırıyordu; mevcut `scheduleSubmit()` akışına bağlandı.
- Hasta harita JS tarafında `entry` oluşturulmadan kullanılıyordu; önce entry oluşturulup sonra dinleyiciler bağlandı.
- `ServiceResult` sadece hata mesajı taşıyordu; işlem sonrası detaylı başarı mesajı için `SuccessMessage` alanı eklendi.
- Yerel Docker yine yanıt vermedi ve `dotnet` hâlâ yok. Build/deploy doğrulaması için DigitalOcean droplet üzerinde devam edilecek.

Kontrol:
- `git diff --check` çalıştırıldı. Sadece CRLF uyarıları var; whitespace hatası yok.

Sonraki adım:
- Değişiklikleri seçili dosyalarla commit et.
- Droplet üzerinde build, migration, seed ve tarayıcı smoke test akışını çalıştır.

## 2026-06-18 3. etap - Hata Giderme, Derleme ve Canlı Dağıtım (Deployment)

Yapılanlar:
- **Konteyner Başlatma Hatası Giderildi:** `docker-compose.yml` içindeki `db` (SQL Server) konteynerine ait `healthcheck` test komutunda `MSSQL_SA_PASSWORD` çevresel değişkeninin tek tırnak (`'`) yerine çift tırnak (`\"`) ile sarmalanması sağlandı. Tek tırnak kullanıldığında kabuk değişkeni çözümleyemiyor ve veritabanı şifresi hatalı olduğundan `unhealthy` kalıyordu. Bu durum düzeltilince veritabanı `healthy` durumuna geçti ve ona bağımlı olan `mentora-app` başarıyla başlatıldı.
- **Veritabanı Migration Hatası Çözüldü:** `20260618090000_SchoolDemoThemeAndRequests.cs` migration dosyasında yer alan `migrationBuilder.UpdateData` çağrılarının EF Core model doğrulamasından geçemediği (`There is no entity type mapped to the table 'Specialties'`) tespit edildi. Bu durum, veritabanı şemasındaki eşleşme sorunlarını bypass etmek için ham SQL çalıştıran `migrationBuilder.Sql` yöntemiyle güncellenerek çözüldü.
- **Uzak Sunucuya Başarıyla Dağıtıldı:** Tüm düzeltmeler lokal git repoya commit edildikten sonra tar arşivi oluşturulup SSH/SFTP paramiko aracıyla DigitalOcean droplet'ına (`164.92.199.17`) yüklendi. Docker image'ları başarıyla derlendi, migrations & seed sorunsuz çalıştırıldı ve uygulama 8080 portunda çalışır duruma getirildi. `curl` testiyle de uygulamanın 200 OK döndüğü ve Türkçe karakterlerin düzgün yerleştiği doğrulandı.

Hatalar / çözümler:
- SQL Server DB başlatılamama sorunu (çözüm: `docker-compose.yml` çift tırnak güncellemesi).
- Specialties tablosu veri güncelleme migration hatası (çözüm: `migrationBuilder.Sql` kullanımı).

Sonraki adım:
- Farklı kullanıcı rolleriyle (hasta, doktor, admin) tarayıcı üzerinden smoke testleri gerçekleştir.
- Projede değişiklikleri git üzerinde push yapabilmek için gerekli erişim anahtarlarını (Token/SSH) tanımla ve push işlemini tamamla.

