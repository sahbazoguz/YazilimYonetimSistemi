# Yazılım Yönetim Sistemi

Uludağ Üniversitesi için geliştirilen .NET 9 tabanlı Yazılım Takip Sistemi; yazılım kataloğunun kamuya açık paylaşılması, birim taleplerinin yönetilmesi ve teknik değerlendirme ile yazılım geliştirme süreçlerinin izlenmesini sağlar.

## Başlangıç

1. `UludagSoftwareTracking.sln` çözüm dosyasını Visual Studio 2022 17.12+ veya .NET 9 SDK (önizleme) içeren ortamda açın.
2. `appsettings.json` içindeki `DefaultConnection` değerini kurumunuzdaki SQL Server bağlantısına göre güncelleyin.
3. Çözümü çalıştırmadan önce `dotnet restore` komutunu çalıştırarak NuGet paketlerini indirin.
4. İlk çalıştırmada `SeedData` sınıfı örnek roller, kullanıcılar ve katalog verileri oluşturur.

## Mimari Genel Bakış

- `Program.cs`: Kimlik doğrulama, yetkilendirme politikaları, yerelleştirme ve servis kayıtlarını içerir.
- `Data/`: Entity Framework Core bağlamı (`ApplicationDbContext`) ve başlangıç verilerini sağlayan `SeedData` sınıfı bulunur.
- `Models/Entities`: Talepler, yazılımlar, kılavuzlar ve projeler için domain modellerini içerir.
- `Models/ViewModels`: Razor görünümlerinde kullanılan veri aktarım modellerini barındırır.
- `Services/`: Katalog, talepler, kılavuzlar, panolar ve kullanıcı rolleri için iş kurallarını kapsayan servis katmanı.
- `Controllers/`: Rol tabanlı iş akışlarını yöneten MVC denetleyicileri.
- `Views/`: Tamamen Türkçe kullanıcı arayüzü sağlayan Razor görünümleri.

## Önemli Özellikler

- Windows/LDAP kimlik doğrulamasıyla uyumlu Negotiate şeması ve veri tabanında tutulan rol eşlemesi.
- Birim Kullanıcısı → Birim Yetkilisi → üç aşamalı teknik değerlendiriciler → Değerlendirme Başkanı → Yazılımcı → Birim test onayı ile tamamlanan uçtan uca talep süreci.
- Yazılım kataloğu, PDF tabanlı teknik/kullanım kılavuzu yönetimi, proje izleme panoları ve talep bazlı sohbet alanı.
- Türkçe tarih, sayı ve doğrulama mesajları ile yerelleştirme ayarları.

## Notlar

- Uygulama `Turkish_CI_AS` sıralama düzeniyle çalışacak şekilde yapılandırılmıştır.
- LDAP/Windows doğrulaması kurum altyapısına göre yapılandırılmalıdır; geliştirme ortamında Negotiate şeması varsayılan davranış sunar.
