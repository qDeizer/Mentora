# Mentora okul projesi yol haritası

Bu dosya projeyi başka bir agente devretmek için tutuluyor. Amaç, okul demosunda ana akışların çalışır görünmesi ve test edilebilir olmasıdır.

## Çalışma kararı

- Yerel makinede `dotnet` PATH içinde yok. Docker komutu da PATH içinde yok, ancak `C:\Program Files\Docker\Docker\resources\bin\docker.exe` var ve Docker Desktop şu an kararsız cevap veriyor.
- Bu yüzden ilk geliştirme aşaması kod ve migration dosyalarını elle düzenleyerek ilerliyor.
- Doğrulama için önce statik kontrol, sonra Docker Desktop toparlanırsa lokal compose, olmazsa DigitalOcean droplet üzerinde Docker build + smoke test yapılacak.
- Proje kodu lokal çalıştırma için uygun kalacak, üretim/deploy için `docker-compose.yml` korunacak.

## Öncelik sırası

1. Demo veri ve seed
   - 2 doktor + 2 hasta oluştur.
   - Demo şifrelerini `asdasd` yap.
   - Online, yüz yüze ve karma randevu örnekleri ekle.
   - Aynı randevuya iki hasta talebi, klinik not, kilitli not, paylaşılmış not, özel teklif örneklerini hazırla.

2. Randevu talebi ve doktor onay akışı
   - Hasta talebine yapılandırılmış ön bilgi alanları ekle.
   - Doktor onay popup'ında diğer başvuran hastaları ve çakışacak slotları göster.
   - Aynı randevudaki diğer bekleyen taleplere de mail/bildirim üret.
   - İşlem sonrası detaylı sonuç mesajı göster.

3. Hasta kesinleşmiş randevuları
   - Hasta için yaklaşan/geçmiş randevu ekranı ekle.
   - Geçmiş tamamlanmış randevuya puan/yorum verebilmesini sağla.
   - Online link sadece onaylı hastanın randevu ekranında görünsün.

## Mevcut durum - 2026-06-18

Tamamlananlar:
- Demo veri seed akışı kod içinden çalışacak şekilde genişletildi; veriler doğrudan SQL ile yazılmıyor.
- Randevu talebi ön bilgi alanları ve doktor onay etki önizlemesi eklendi.
- Onaylanan randevu sonrası aynı slot ve çakışan slot taleplerini otomatik reddeden servis akışı eklendi.
- Hasta “Randevularım” ekranı, değerlendirme formu ve `.ics` takvim çıktısı eklendi.
- Tema/görünüm yoğunluğu ayarı eklendi.
- Görünen metinler Türkçeleştirildi; “Online” kullanıcı tarafında “Çevrim içi” olarak standartlaştırıldı.
- `git diff --check` temiz; sadece Windows CRLF uyarıları var.

Kalan doğrulama:
- Yerel `dotnet` olmadığı ve Docker Desktop cevap vermediği için gerçek derleme/test droplet üzerinde yapılmalı.
- Droplet testinde öncelik: `docker compose build`, migration/seed, login, hasta talep oluşturma, doktor onay, hasta randevu/puanlama, tema değişimi.

Risk notları:
- Local git çalışma ağacında kullanıcıya ait görünen silinmiş `screenshoots/*.png` dosyaları ve untracked snapshot dosyaları var. Bunlar bu işin parçası değil; stage edilmemeli.
- GitHub HTTPS push interaktif kullanıcı adı istediği için ilk push başarısız olmuştu. Credential hazır olana kadar commitler lokal kalabilir.

4. Tema ve Türkçe görünüm
   - Açık/koyu/sistem teması ve rahat/kompakt görünüm seçimi ekle.
   - Giriş yapılmadan önce de localStorage ile tema korunur.
   - Giriş yapılmış kullanıcıda tercih kullanıcı kaydına yazılır.
   - Görünen İngilizce ve Türkçe karaktersiz metinler temizlenir.

5. Test ve deploy
   - Build, Docker build, DB migration ve temel tarayıcı akışları test edilir.
   - Son aşamada DigitalOcean droplet üzerinde compose ile yayınlanır.
   - Test edilen hesaplar ve test sonucu final rehbere eklenir.

## Kabul için kısa ana akış

- Admin giriş yapar, doktorları görür.
- Doktor randevu oluşturur.
- Hasta randevu arar, filtreler ve talep gönderir.
- İki hasta aynı randevuya talep gönderir.
- Doktor birini onaylayınca diğer talep otomatik reddedilir.
- Bildirim/outbox kayıtları oluşur.
- Hasta yaklaşan randevusunu görür.
- Doktor klinik not yazar, hasta kilitli notu okuyamaz, paylaşım geri alınca erişim kapanır.
- UI metinleri Türkçe ve okunabilir olur.
