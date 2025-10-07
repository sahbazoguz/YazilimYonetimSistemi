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
        public const string DegerlendiriciBir = "Değerlendirici 1";
        public const string DegerlendiriciIki = "Değerlendirici 2";
        public const string DegerlendiriciUc = "Değerlendirici 3";
        public const string DegerlendirmeBaskani = "Değerlendirme Başkanı";
        public const string Yazilimci = "Yazılımcı";
        public const string EkipLideri = "Ekip Lideri";
        public const string TestYazilimcisi = "Test Yazılımcısı";
        public const string Admin = "Admin";
    }

    public static class Policies
    {
        public const string RequireBirimKullanicisi = "RequireBirimKullanicisi";
        public const string RequireBirimYetkilisi = "RequireBirimYetkilisi";
        public const string RequireDegerlendirici = "RequireDegerlendirici";
        public const string RequireBaskan = "RequireBaskan";
        public const string RequireYazilimci = "RequireYazilimci";
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireWorkflowEditor = "RequireWorkflowEditor";
    }
}
