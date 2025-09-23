namespace UludagSoftwareTracking.Services.Security;

public static class RoleConstants
{
    public static class Roles
    {
        public const string Misafir = "Misafir";
        public const string Ogrenci = "Öğrenci";
        public const string Personel = "Personel";
        public const string BirimKullanicisi = "Birim Kullanıcısı";
        public const string BirimYetkilisi = "Birim Yetkilisi";
        public const string BilgiIslemDegerlendirmeEkibi = "Bilgi İşlem Değerlendirme Ekibi";
        public const string YazilimEkibiLideri = "Yazılım Ekibi Lideri";
        public const string Yazilimci = "Yazılımcı";
        public const string Admin = "Admin";
    }

    public static class Policies
    {
        public const string RequireBirimKullanicisi = "RequireBirimKullanicisi";
        public const string RequireBirimYetkilisi = "RequireBirimYetkilisi";
        public const string RequireItTeam = "RequireItTeam";
        public const string RequireYazilimEkibi = "RequireYazilimEkibi";
        public const string RequireAdmin = "RequireAdmin";
    }
}
