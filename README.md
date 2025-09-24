# Yazılım Yönetim Sistemi

Üniversite bünyesindeki yazılım taleplerinin kaydı, takibi ve yönetimini sağlayan örnek bir ASP.NET Core MVC uygulamasıdır. LDAP tabanlı kurumsal kimlik doğrulaması desteklenir; test ortamlarında ise kullanıcılar `UserProfiles` tablosunda tutulan hash’lenmiş parolalar ile doğrulanır.

## Proje Yapısı

- **Çatıyapı:** .NET 8 MVC (Razor Views), Entity Framework Core
- **Veri Katmanı:** `ApplicationDbContext` üzerinden `UserProfiles` tablosu
- **Kimlik Doğrulama:** LDAP (Active Directory) + hash’lenmiş parola fallback’i
- **Rol Yönetimi:** Her zaman `UserProfiles` tablosundan okunur (örn. Birim Kullanıcısı, Birim Yetkilisi, Yazılımcı, Ekip Lideri, Admin)

## Çalıştırma

1. .NET 8 SDK yüklü olduğundan emin olun.
2. `appsettings.Development.json` dosyasında yer alan `DefaultConnection` bağlantı dizesini kendi SQL Server örneğinize göre güncelleyin. Geliştirme ortamı için LocalDB varsayılan olarak tanımlanmıştır.
3. Depo kökünde aşağıdaki komutları kullanın:
   ```bash
   dotnet restore
   dotnet run --project src/YazilimYonetimSistemi.Web
   ```
4. Uygulama varsayılan olarak `https://localhost:5001` adresinden yayın yapar.

> Not: Geliştirme ortamı için `appsettings.json` dosyasında bağlantı dizesi boş bırakılmıştır ve uygulama otomatik olarak EF Core InMemory sağlayıcısını kullanır. SQL Server veya SQLite tercih ediyorsanız `ConnectionStrings:DefaultConnection` değerini güncellemeniz yeterlidir.

## Varsayılan Giriş Bilgileri (Test Ortamı)

Uygulama ilk çalıştığında `SeedData` sınıfı aşağıdaki kullanıcıyı oluşturur:

| Rol   | E-posta              | Parola        | Birim |
|-------|---------------------|---------------|-------|
| Admin | `admin@example.com` | `ChangeMe!123` | Bilgi İşlem |

Bu kullanıcı `AuthenticationProvider.Internal` olarak işaretlenmiştir ve parola doğrulaması PBKDF2 ile hash’lenmiş şekilde yapılır.
İlk kurulumda ayrıca "Bilgi İşlem Daire Başkanlığı" isminde aktif bir birim oluşturulur ve varsayılan kullanıcı bu birime atanır.

## LDAP Konfigürasyonu

`appsettings.json` içerisindeki `Authentication` ve `Ldap` bölümleri LDAP kullanımını yönetir:

```json
"Authentication": {
  "UseLdap": true,
  "AllowPasswordFallback": true
},
"Ldap": {
  "Enabled": true,
  "Server": "ldap.universite.local",
  "Port": 389,
  "UseSsl": false,
  "Domain": "universite.local",
  "BaseDn": "DC=universite,DC=local",
  "TimeoutSeconds": 30,
  "IgnoreCertificateErrors": false
}
```

- **UseLdap:** LDAP doğrulamasını aktif eder. Varsayılan olarak test senaryoları için `false` gelir.
- **AllowPasswordFallback:** LDAP başarısız olsa bile yerel parola ile doğrulamaya izin verir.
- **Ldap.Enabled:** LDAP bağlantısı kurulup kurulmayacağını belirler.

Üretim ortamında LDAP parametrelerini güncelleyip `UseLdap` ve `Ldap.Enabled` değerlerini `true` yapmanız yeterlidir. Başarılı LDAP doğrulaması sonrası rol bilgisi yine `UserProfiles` tablosundan okunur.

## Öne Çıkan Bileşenler

- `UserAuthenticationService`: LDAP + yerel hash doğrulama akışını yönetir.
- `Pbkdf2PasswordHashingService`: PBKDF2 tabanlı parola hash’leme ve doğrulama.
- `LdapAuthenticationService`: Active Directory ile konuşarak kullanıcıyı doğrular.
- `AccountController`: Mail + şifre ile giriş ekranı ve cookie tabanlı oturum yönetimini sağlar.

## Geliştirme Notları

- Tüm servisler dependency injection üzerinden kayıtlıdır.
- `Authorization` için uygulama genelinde cookie kimlik doğrulaması kullanılmaktadır.
- Geliştirme ortamında HTTPS zorunlu olup, login ekranı `Account/Login` adresindedir.

