# Uludağ Üniversitesi Yazılım Takip Sistemi (CODEXE) - Geliştirici Rehberi

## 1. Amaç ve Kapsam
Uludağ Üniversitesi Yazılım Takip Sistemi (kısa adıyla CODEXE), üniversite genelindeki yazılım taleplerinin başvuru, değerlendirme ve geliştirme süreçlerini tek bir platformda yönetmek için tasarlanmıştır. Sistem, kamuya açık yazılım kataloğu ile yetkilendirilmiş kullanıcılar için talep ve değerlendirme panellerini bir araya getirir. Bu rehber, CODEXE uygulamasının uçtan uca nasıl planlanacağı, geliştirileceği ve işletileceği hakkında ayrıntılı yönlendirmeler sunar.

## 2. Teknoloji ve Altyapı
- **Uygulama Çatısı:** .NET 9 MVC + Razor Pages hibrit yaklaşımı
- **Veritabanı:** Microsoft SQL Server (Turkish collation: `SQL_Latin1_General_CP1_CI_AS`)
- **Kimlik Doğrulama:** Windows Authentication + LDAP entegrasyonu
- **Sunucu Yapısı:** IIS üzerinde host edilen kurumsal intranet uygulaması
- **Ön Yüz:** Bootstrap 5 tabanlı tema, tüm metinler Türkçe
- **Sürüm Takibi:** Git (main branch üzerinde geliştirme, feature branch opsiyonel)
- **CI/CD Önerisi:** Azure DevOps Pipeline veya GitHub Actions (derleme + test + yayın)

## 3. Katmanlı Mimari
```
/Presentation
    /Controllers (MVC)
    /Pages (Razor Pages)
    /Views
    /Resources (Türkçe yerelleştirme dosyaları)
/Application
    /Services (iş kuralları)
    /DTOs
    /Validators (FluentValidation)
    /Workflows
/Domain
    /Entities
    /ValueObjects
    /Enums (Rol kodları vb.)
/Infrastructure
    /Persistence (EF Core DbContext, Migrations)
    /Repositories
    /Identity (LDAP, Windows Auth adapter)
    /FileStorage (PDF kılavuzları için)
/Common
    /Extensions
    /Localization
    /Notifications (E-posta şablonları)
```
Bu yapı, SOLID prensiplerine uyum ve test edilebilirlik için önerilir. Presentation katmanında misafir katalog erişimi için Razor Pages, yönetim panelleri için MVC Controller + View Model yaklaşımı kullanılabilir.

## 4. Modüller ve Özellikler
### 4.1 Yazılım Kataloğu (Herkese Açık)
- Yazılım listesi: alfabetik, kategori ve tarih bazlı sıralama
- Arama: yazılım adına göre
- Filtreler: bölüm ve teknoloji tipine göre
- Detay sayfası: açıklama, sürüm, teknik gereksinim, sorumlu birim, ekran görüntüleri
- Kullanım kılavuzu PDF önizleme/indirme
- QR kod üretimi ve gömme bağlantılar

### 4.2 Talep Yönetimi (Birim Kullanıcısı ve Üzeri)
- Talep oluşturma formu (zorunlu alan kontrolleri)
- Talep durum akışı: `Taslak → Birim Onayı Bekleniyor → Birim Onayı/Reddetme → Değerlendirici 1/2/3 İncelemesi → Başkan Onayı Bekliyor → Geliştirme → Tamamlandı → Birim Test Onayı`
- Talep geçmişi ve günlük kaydı (audit trail)
- İlgili dosyaların (doküman, ekran görüntüsü) yüklenmesi

### 4.3 Değerlendirme ve Onay
- Birim Yetkilisi için departman talepleri listesi ve onay ekranı
- Değerlendirici 1/2/3 için teknik uygunluk ve mevcut yazılım kontrolü ekranı
- Değerlendirme Başkanı için üç değerlendirmenin çıktısını birleştirip karar verme paneli
- Yazılımcılar için görev listesi, algoritma taslağı, kılavuz yükleme ve mesajlaşma modülü

### 4.4 Doküman Yönetimi
- Kullanım kılavuzu versiyonlama (Birim Kullanıcısı sorumluluğunda)
- Teknik kılavuzların oluşturulması ve paylaşımı (Yazılım Ekibi tarafından)
- PDF üretimi (wkhtmltopdf veya .NET PDF kütüphanesi ile)
- Doküman durum kayıtları (taslak, yayınlandı, arşivlendi)

### 4.5 Raporlama ve Gösterge Panelleri
- Rol bazlı panolar (her biri Türkçe başlıklar ile)
- Filtreli raporlar (tarih aralığı, bölüm, durum)
- Excel/PDF çıktıları
- İstatistik görselleri (Chart.js veya alternatif)

## 5. Roller ve Yetkiler
| Rol | Açıklama | Temel Yetkiler |
| --- | --- | --- |
| Misafir | Oturum açmamış kullanıcı | Yazılım kataloğunu ve kılavuzları görüntüleme |
| Öğrenci | LDAP doğrulamalı öğrenci | Misafir yetkileri, talep oluşturamaz |
| Personel | LDAP doğrulamalı personel | Misafir yetkileri, talep oluşturamaz |
| Birim Kullanıcısı | Birim yetkilisinin atadığı kullanıcı | Talep oluşturma, taleplerini izleme, kılavuz yükleme |
| Birim Yetkilisi | Departman sorumlusu | Departman taleplerini görüntüleme, onaylama/red, kılavuz yönetimi |
| Değerlendirici 1 | Teknik değerlendirme uzmanı 1 | Talepleri değerlendirme, uygunluk notu girme, mevcut yazılım önerisi |
| Değerlendirici 2 | Teknik değerlendirme uzmanı 2 | Talepleri değerlendirme, uygunluk notu girme, mevcut yazılım önerisi |
| Değerlendirici 3 | Teknik değerlendirme uzmanı 3 | Talepleri değerlendirme, uygunluk notu girme, mevcut yazılım önerisi |
| Değerlendirme Başkanı | Teknik kurul başkanı | Tüm değerlendirmeleri görme, final karar verme, yazılımcıya yönlendirme |
| Yazılımcı | Yazılım geliştirme personeli | Atanmış projeleri yönetme, algoritma taslağı oluşturma, kılavuz yükleme, mesajlaşma |
| Admin | Sistem yöneticisi | Tüm sistem yönetimi, rol atamaları, ayarlar, raporlar |

Rol tanımları için ASP.NET Core Identity yerine LDAP gruplarından okunan claim'ler + uygulama içi rol tablosu birlikte kullanılabilir. Rol kontrolleri Authorization Policy ile yönetilmelidir.

## 6. Veri Modeli (Önerilen Tablolar)
### 6.1 Temel Tablolar
- `Kullanicilar (UserId, AdSoyad, Email, SicilNo, RolId, BirimId, Durum)`
- `Roller (RolId, RolAdi, Aciklama)`
- `Birimler (BirimId, Ad, Eposta, Telefon)`
- `Yazilimlar (YazilimId, Ad, Aciklama, KategoriId, TeknolojiTipiId, SorumluBirimId, YayimTarihi, Durum)`
- `YazilimKategorileri (KategoriId, Ad)`
- `TeknolojiTipleri (TeknolojiTipiId, Ad)`
- `YazilimKullanicilari (YazilimId, BirimId)` (bölüm bazlı kullanım)

### 6.2 Talep Süreci Tabloları
- `Talepler (TalepId, OlusturanKullaniciId, BirimId, Baslik, Aciklama, Oncelik, Durum, OlusturmaTarihi)`
- `TalepDurumGecmisleri (DurumId, TalepId, YeniDurum, Aciklama, DegistirenKullaniciId, Tarih)`
- `TalepEkDosyalari (DosyaId, TalepId, DosyaYolu, Aciklama, YuklemeTarihi)`
- `TalepAtamalari (AtamaId, TalepId, KullaniciId, Rol, Tarih)`

### 6.3 Doküman Yönetimi
- `KullanimKilavuzlari (KilavuzId, YazilimId, Versiyon, DosyaYolu, OlusturanId, YayimTarihi, Durum)`
- `TeknikKilavuzlar (TeknikKilavuzId, YazilimId, Versiyon, DosyaYolu, OlusturanId, YayimTarihi, Durum)`

### 6.4 Mesajlaşma ve Bildirim
- `TalepMesajlari (MesajId, TalepId, GonderenId, Mesaj, Tarih)`
- `BildirimSablonlari (SablonId, Kod, Baslik, Govde)`
- `BildirimKayitlari (BildirimId, KullaniciId, TalepId, SablonId, GonderimTarihi, Durum)`

## 7. İş Akışları
### 7.1 Talep Oluşturma
1. Birim Kullanıcısı "Talep Oluştur" formunu doldurur.
2. Zorunlu alanlar: Başlık, Açıklama, İlgili Yazılım (yeni/güncelleme), Öncelik, Beklenen Sonuç.
3. Kaydetme ile birlikte talep durumu "Birim Onayı Bekleniyor" olur.
4. Sistem, ilgili Birim Yetkilisine "Yeni Talep Bildirimi" e-postası gönderir.

### 7.2 Birim Onayı
1. Birim Yetkilisi "Onay Bekleyen Talepler" ekranında talepleri görüntüler.
2. Onaylıyorsa durum "Değerlendirmede" olur ve üç teknik değerlendiriciye bildirim gider.
3. Reddederse "Red" durumu ile kapanır, gerekçe zorunludur.

### 7.3 Teknik Değerlendirme
1. Değerlendirici 1, 2 ve 3 talebi sırayla inceleyerek görüş ve sonuçlarını kaydeder.
2. Her değerlendirme sonucunda aşağıdakilerden biri seçilir:
   - Mevcut Yazılım Yönlendirmesi: Talep kapatılır, yönlendirme notu eklenir.
   - Yeni Geliştirme: Talep başkan onayına hazırlık aşamasına geçer.
   - Uygun Değil: Talep gerekçesiyle sonlandırılır.
3. Üç değerlendirici de raporlarını tamamladığında durum "Başkan Onayı Bekliyor" olur ve değerlendirme başkanına bildirim gider.

### 7.4 Geliştirme Süreci
1. Başkan onayından sonra talep için proje kaydı oluşturulur ve geliştirici görevlendirilir.
2. Yazılımcı algoritma taslağını hazırlar, teknik ve kullanım kılavuzu bağlantılarını günceller, sohbet üzerinden birimle iletişim kurar.
3. Geliştirme tamamlandığında durum "Tamamlandı" olur ve birim test onayı beklenir.
4. Birim test onayıyla talep kapatılır ve proje kaydı yayıma alınır.

## 8. Arayüz ve Yerelleştirme İlkeleri
- Tüm başlıklar, buton etiketleri, menüler ve hata mesajları Türkçe yazılmalıdır.
- Tarih formatı: `dd/MM/yyyy`, saat formatı: `HH:mm`.
- Sayı formatları: `1.234,56`.
- Razor view'larında `@CultureInfo("tr-TR")` kullanımı ve `RequestLocalizationOptions` ile kültür ayarı.
- Kimlik doğrulama sonrası kullanıcı adı üst menüde "Hoş geldiniz, {Ad}" olarak gösterilir.
- Yetkisiz erişimlerde "Erişim izniniz yok" mesajı.
- Form doğrulamalarında: "Lütfen zorunlu alanları doldurunuz".

## 9. E-posta ve Bildirim Şablonları
- **Yeni Talep Bildirimi:** "{BirimAdi} biriminden yeni talep oluşturuldu."
- **Onay Bekleyen Talepler:** Günlük özet e-postası.
- **Talep Durum Güncellemesi:** Talep durumu değiştiğinde otomatik gönderim.
- E-posta içerikleri HTML + Türkçe karakter desteği ile hazırlanmalıdır.

## 10. Güvenlik ve Yetkilendirme
- Windows Authentication ile giriş yapan kullanıcıların kimlik bilgileri LDAP üzerinden doğrulanır.
- Uygulama içinde `AuthorizationPolicy` tanımları:
- `Policy = "RequireBirimKullanicisi"` → Birim Kullanıcısı, Birim Yetkilisi, Admin
- `Policy = "RequireBirimYetkilisi"` → Birim Yetkilisi, Admin
- `Policy = "RequireDegerlendirici"` → Değerlendirici 1/2/3, Admin
- `Policy = "RequireBaskan"` → Değerlendirme Başkanı, Admin
- `Policy = "RequireYazilimci"` → Yazılımcı, Admin
- `Policy = "RequireAdmin"` → Admin
- Controller ve PageModel'lerde `[Authorize(Policy = "...")]` kullanımı.
- Talep erişiminde veri satırı düzeyinde filtreleme (ör. EF Core Global Query Filter veya servis katmanında kontrol).

## 11. Doküman Depolama
- Dokümanlar için `wwwroot\kilavuzlar\{yil}\{yazilimId}\` dizin yapısı önerilir.
- Dosya isimlerinde Türkçe karakter desteği için `Path.GetInvalidFileNameChars` temizliği yapılmalıdır.
- Versiyonlama: `Kilavuz-V{versiyon}.pdf` formatı.
- Büyük dosyalar için Azure Blob Storage veya dosya sunucusu entegrasyonu değerlendirilebilir.

## 12. Raporlama
- Yönetim raporları: talep sayısı, durum dağılımı, bölüm bazlı istatistikler.
- İhracat formatları: Excel (EPPlus), PDF (QuestPDF).
- Grafikler: Bar, çizgi ve pasta grafikler.
- Filtreler: tarih aralığı, bölüm, durum, teknoloji tipi.

## 13. Test Stratejisi
- **Birim Testleri:** Application katmanı servisleri için xUnit + Moq.
- **Entegrasyon Testleri:** API Controller'ları ve veri erişimi.
- **UI Testleri:** Playwright ile rol bazlı kritik senaryolar.
- **Kabul Testleri:** Her rol için kullanıcı hikâyeleri (örn. Birim Kullanıcısı talep oluşturma).
- **Performans Testleri:** JMeter ile 200 eş zamanlı kullanıcı senaryosu.

## 14. Yayın ve Devreye Alma
1. Staging ortamında LDAP ve veritabanı bağlantılarının test edilmesi.
2. IIS üzerinde uygulama havuzunun .NET CLR v4.0 ve 64-bit olarak yapılandırılması.
3. Web.config'te Windows Authentication'ın etkin olması.
4. Veritabanı migration'larının `dotnet ef database update` ile uygulanması.
5. İlk kullanıcıların (Admin, Birim Yetkilileri) tanımlanması.

## 15. Yol Haritası (Fazlar)
- **Faz 1:** Türkçe arayüz, rol bazlı yetkilendirme, temel talep iş akışı, yazılım kataloğu.
- **Faz 2:** Departman panoları, doküman yönetimi, detaylı raporlama.
- **Faz 3:** Performans optimizasyonu, gelişmiş arama, dış entegrasyonlar (ör. EBYS, e-posta gateway).

## 16. Sık Karşılaşılan Sorular
- **LDAP bağlantısı çalışmıyor:** AppSettings'de LDAP sunucu adresini ve `UserDomainName` değerini kontrol edin.
- **Türkçe karakterler bozuk görünüyor:** Tüm dosyalar UTF-8 kodlamalı olmalı ve SQL Server bağlantısı `Trusted_Connection` ile sağlanmalıdır.
- **Talep göremiyorum:** Kullanıcının rolü ve birim yetkisi doğru atanmış mı kontrol edin, ayrıca veri satırı filtrelerini inceleyin.

## 17. Ek Kaynaklar
- Microsoft Docs: [ASP.NET Core MVC ile Web Uygulamaları](https://learn.microsoft.com/aspnet/core/mvc)
- Microsoft Docs: [Windows Authentication ve LDAP](https://learn.microsoft.com/aspnet/core/security/authentication/windowsauth)
- EF Core: [Global Query Filters](https://learn.microsoft.com/ef/core/querying/filters)

Bu rehber, CODEXE geliştirme ekibinin projeyi tutarlı, güvenli ve sürdürülebilir şekilde inşa etmesine yardımcı olmak amacıyla hazırlanmıştır. Tüm kullanıcı arayüzü ve içeriklerin Türkçe olarak tutulması, rol bazlı yetkilerin doğru kurgulanması ve süreçlerin üniversite ihtiyaçlarına uygun şekilde yapılandırılması kritik öneme sahiptir.

## 18. Kullanıcı Hikâyeleri ve Kabul Kriterleri
Her rol için temel kullanıcı hikâyeleri ve kabul kriterleri net şekilde tanımlanmalıdır. Örnek senaryolar:

### Misafir / Öğrenci / Personel
- **Katalog görüntüleme:** "Misafir olarak yazılım kataloğuna girdiğimde alfabetik sıralamayı görebilmeliyim." → *Kabul:* Alfabetik sıralı liste, filtreler doğru çalışır.
- **Kılavuz indirme:** "Bir yazılım detay sayfasına girdiğimde PDF kılavuzunu önizleyip indirebilmeliyim." → *Kabul:* PDF yeni sekmede açılır ve UTF-8 karakterleri doğru gösterir.

### Birim Kullanıcısı
- **Talep oluşturma:** "Birim kullanıcısı olarak zorunlu alanları doldurup talep kaydettiğimde durum 'Birim Onayı Bekleniyor' olmalıdır." → *Kabul:* Talep listesinde yeni kayıt görünür, e-posta tetiklenir.
- **Kılavuz yönetimi:** "Var olan kılavuzun yeni versiyonunu yüklediğimde eski versiyon arşivlenmelidir." → *Kabul:* Versiyon numarası artar, önceki dosya arşiv klasörüne taşınır.

### Birim Yetkilisi
- **Onay/Red:** "Birim yetkilisi olarak talebi reddedersem gerekçe alanının dolu olması zorunlu olmalıdır." → *Kabul:* Gerekçe boşsa kaydetme engellenir.
- **Raporlama:** "Departman raporlarını CSV dışa aktardığımda Türkçe karakterler bozulmamalıdır." → *Kabul:* UTF-8 BOM ile dışa aktarım yapılır.

### Değerlendirici 1/2/3
- **Teknik inceleme:** "Değerlendirme ekranında talep detayını açtığımda önceki onay/ret notlarını görebilmeliyim." → *Kabul:* Talep geçmişi paneli yüklenir.
- **Mevcut yazılım yönlendirme:** "Talebi mevcut yazılıma yönlendirdiğimde ilgili yazılım linki zorunlu olmalıdır." → *Kabul:* Link boşsa doğrulama hatası döner.

### Değerlendirme Başkanı
- **Karar verme:** "Üç değerlendiricinin raporlarını gördüğümde talebi onaylayıp geliştirmeye aktarabilmeliyim." → *Kabul:* Eksik değerlendirme varsa kaydetme engellenir.
- **Reddetme:** "Talebi reddedersem gerekçenin talep detayında görünmesi gerekir." → *Kabul:* Red gerekçesi talep kartında listelenir.

### Yazılımcı
- **Görev ve algoritma:** "Atandığım projede algoritma taslağını güncellediğimde değişiklik talep detayında görünmelidir." → *Kabul:* Algoritma alanı güncel metinle yenilenir.
- **Mesajlaşma:** "Yazılımcı olarak proje mesaj paneline yeni mesaj girdiğimde karşı tarafa bildirim gitmelidir." → *Kabul:* SignalR bildirimi ve e-posta tetiklenir.

### Admin
- **Rol atama:** "Admin olarak yeni kullanıcıyı 'Birim Yetkilisi' rolüne atadığımda rol atama kaydı tutulmalıdır." → *Kabul:* Audit kaydı `TalepDurumGecmisleri` benzeri tabloda saklanır.
- **Sistem ayarları:** "LDAP sunucu adresini güncellediğimde sistem yeniden başlatılmadan yapılandırma etkin olmalıdır." → *Kabul:* `IOptionsSnapshot` üzerinden runtime güncelleme doğrulanır.

## 19. Ekran Haritası ve Navigasyon
Aşağıdaki tablo, temel ekranları ve erişim yollarını özetler:

| Modül | Sayfa / Endpoint | Rol | Açıklama |
| --- | --- | --- | --- |
| Katalog | `/` (Razor Page) | Herkes | Yazılım listesi, filtreler |
| Katalog | `/yazilim/{id}` | Herkes | Yazılım detay, kılavuz önizleme |
| Talepler | `/birim/talep-olustur` | Birim Kullanıcısı | Yeni talep formu |
| Talepler | `/birim/taleplerim` | Birim Kullanıcısı | Talep listesi |
| Onay | `/birim-yetkilisi/onay-bekleyenler` | Birim Yetkilisi | Departman onay ekranı |
| Değerlendirme | `/Requests/DegerlendirmeBekleyen` | Değerlendirici 1/2/3 | Teknik değerlendirme havuzu |
| Başkan Onayı | `/Requests/BaskanOnayBekleyen` | Değerlendirme Başkanı | Başkan onayı kuyruğu |
| Projeler | `/Dashboard` | Yazılımcı | Proje panosu |
| Dokümanlar | `/dokuman/kilavuzlar` | Yetkili roller | Kılavuz yönetimi |
| Yönetim | `/admin/kullanicilar` | Admin | Kullanıcı/rol yönetimi |

Teknik ve kullanım kılavuzları yalnızca PDF formatında yüklenir; dosyalar `wwwroot/uploads/kilavuzlar` dizininde saklanır ve dosya boyutu 20 MB ile sınırlandırılır.

Mobil uyum için Bootstrap grid yapısı kullanılmalı, en kritik eylemler (talep oluştur, onayla) ekranın üst kısmında belirgin butonlarla sunulmalıdır.

## 20. Servis ve API Sözleşmeleri
Uygulama içi servislerin kontratları Swagger/OpenAPI ile belgelenmelidir. Önerilen REST uç noktaları:

- `GET /api/yazilimlar` → Listeleme, sorgu parametreleri: `arama`, `kategoriId`, `teknolojiTipiId`, `sirala`.
- `GET /api/yazilimlar/{id}` → Detay bilgisi, ilgili kılavuz meta verileri dahil.
- `POST /api/talepler` → Birim Kullanıcısı talepleri. Body doğrulama: maksimum 500 karakter açıklama, öncelik enum (`Dusuk`, `Orta`, `Yuksek`).
- `PUT /api/talepler/{id}/onay` → Birim Yetkilisi onayı, body: `Durum`, `Gerekce`.
- `POST /api/talepler/{id}/degerlendirme` → BİD ekip kararı, body: `Sonuc` (`MevcutYazilimaYonlendir`, `Gelistirme`, `Red`), `Notlar`.
- `POST /api/projeler/{id}/gorevler` → Yazılımcı görev planlama ve iş atama.
- `POST /api/mesajlar` → Yazılımcı mesaj gönderme, SignalR hub tetiklenir.

Her endpoint için `Authorization` header zorunlu, rol tabanlı policy kontrolleri middleware seviyesinde yapılmalıdır. Yanıtlar `application/json; charset=utf-8` formatında, hata durumlarında RFC 7807 Problem Details kullanımı önerilir.

## 21. Zamanlanmış Görevler ve Bildirim Otomasyonu
- `Hangfire` veya `Quartz.NET` ile günlük `Onay Bekleyen Talepler` özeti gönderimi.
- Haftalık raporların (talep sayıları, tamamlanma oranları) otomatik oluşturulması ve ilgili rollere e-posta ile paylaşımı.
- Kılavuz versiyonlarının son kullanım tarihine 30 gün kala hatırlatma bildirimi.
- LDAP senkronizasyonu için gece yarısı çalışan kullanıcı güncelleme job'ı (pasif olanların işaretlenmesi).

Görev durumları `BackgroundJobs` adlı bir tabloda saklanmalı, başarısızlık durumunda tekrar deneme ve loglama yapılmalıdır.

## 22. Performans ve Ölçeklenebilirlik Stratejileri
- Yazılım kataloğu için `OutputCache` ile 15 dakikalık önbellekleme.
- Talep listelerinde sayfalama (`PagedList`) ve sunucu tarafı filtreleme.
- Dosya yükleme boyut sınırı (varsayılan 20 MB) ve virüs taraması entegrasyonu.
- SQL Server'da indeksleme: `Talepler(Durum, BirimId)`, `TalepDurumGecmisleri(TalepId, Tarih)`.
- Uygulama içi metrikler (istek süresi, hata oranı) için `Prometheus` + `Grafana` entegrasyonu.

## 23. Loglama, İzleme ve Denetim
- `Serilog` ile dosya + SQL Sink kombinasyonu, log formatı JSON.
- Kritik işlemler (talep oluşturma, onay, rol atama) `AuditLog` tablosuna yazılır.
- Yetkisiz erişim denemeleri için uyarı seviyesi loglar ve e-posta bildirimleri.
- `HealthChecks` middleware ile `/health` endpoint'i, LDAP/SQL/dosya sistemi kontrolleri.

## 24. Yapılandırma Yönetimi
- Ortam bazlı `appsettings.{Environment}.json` dosyaları, gizli bilgiler için `Azure Key Vault` veya işletim sistemi gizli değişkenleri.
- LDAP, e-posta sunucusu, dosya yolu gibi ayarlar `IOptions` pattern'iyle enjekte edilir.
- Konfigürasyon değişikliklerinde `OptionsMonitor` kullanılarak canlıda güncelleme.

## 25. Yedekleme ve Felaket Kurtarma
- SQL Server veritabanı için günlük diferansiyel, haftalık tam yedekleme planı.
- Kılavuz dosyaları için depolama replikasyonu (en az iki sunucu). Gerektiğinde `Azure Storage` geo-replication.
- Uygulama ayar dosyalarının Git üzerinden versiyonlanması, hassas değerlerin kasada saklanması.
- Felaket durumunda 4 saat içinde temel fonksiyonları ayağa kaldırmayı hedefleyen RTO/RPO tanımları.

## 26. Güvenlik Kontrolleri (Detay)
- `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` header'larının IIS üzerinden ayarlanması.
- Tüm formlarda `AntiForgeryToken` kullanımı, AJAX isteklerinde header gönderimi.
- Dosya yüklemelerinde uzantı beyaz listesi (`.pdf`, `.docx`, `.xlsx`, `.png`, `.jpg`).
- Parola veya gizli veri tutulmamasına rağmen, kullanıcı oturum bilgileri şifreli cookie'de saklanmalı.
- LDAP bağlantılarında `LDAPS` (SSL) zorunlu.

## 27. Geliştirme Standartları ve Süreçler
- Kod incelemeleri için `pull request` şablonu: kapsam, test sonuçları, Türkçe UI kontrol listesi.
- `dotnet format` ve `StyleCop` ile kod standartlarının otomatik kontrolü.
- Work item takibi: Azure Boards'da epik → özellik → görev hiyerarşisi, talep numarası ile ilişkilendirme.
- Continuous Integration aşamasında `dotnet test`, `sonarscanner`, `dotnet publish` adımları.

## 28. Eğitim ve Kullanıcı Adaptasyon Planı
- Birim Kullanıcıları için 2 saatlik çevrim içi eğitim, talep oluşturma ve kılavuz yönetimine odaklı.
- Birim Yetkilileri için vaka bazlı onay/red atölyesi.
- BİD Ekibi için teknik değerlendirme rehberleri ve mevcut yazılım envanteri eğitimi.
- Portal içerisinde "Sıkça Sorulan Sorular" ve video kılavuzlar (Türkçe altyazılı).

## 29. Riskler ve Azaltma Önlemleri
- **LDAP erişim kesintisi:** Uygulama, daha önce oturum açmış kullanıcılar için belirli süreli (4 saat) token geçerliliği sağlar, kesinti sürecinde sadece katalog erişimi garantilenir.
- **Talep patlaması (yüksek başvuru):** Kuyruklama ve önceliklendirme mekanizması devreye alınır, kritik talepler için SLA tanımlanır.
- **Doküman versiyon uyuşmazlığı:** Check-in/out mekanizması ve versiyon notu zorunluluğu ile kontrol sağlanır.
- **Geliştirici kaynak yetersizliği:** Dış kaynak havuzu ve üniversite içi part-time yazılımcı listesi tutulur.

## 30. Sık Kullanılan CLI Komutları
- `dotnet new mvc -n CodeXe.Web` → Başlangıç projesi oluşturma.
- `dotnet ef migrations add IlkSurum` → İlk migration.
- `dotnet ef database update` → Veritabanını güncelleme.
- `dotnet test` → Testleri çalıştırma.
- `npm install && npm run build` → Ön yüz varlıklarını derleme (varsa).

Bu ek rehber bölümleri, proje ekiplerinin gereksinimleri netleştirmesine, geliştirme sürecini disiplinli yürütmesine ve kurum beklentilerini karşılayan bir CODEXE çözümü sunmasına yardımcı olacaktır.
