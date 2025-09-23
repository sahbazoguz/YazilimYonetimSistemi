namespace UludagSoftwareTracking.Models.Entities;

public enum UserRole
{
    Misafir = 0,
    Ogrenci = 1,
    Personel = 2,
    BirimKullanicisi = 3,
    BirimYetkilisi = 4,
    BilgiIslemDegerlendirmeEkibi = 5,
    YazilimEkibiLideri = 6,
    Yazilimci = 7,
    Admin = 8
}

public enum ManualType
{
    KullanimKilavuzu = 0,
    TeknikKilavuz = 1
}

public enum RequestStatus
{
    Taslak = 0,
    OnayBekleniyor = 1,
    Onaylandi = 2,
    Reddedildi = 3,
    Degerlendirmede = 4,
    Gelistirmede = 5,
    Tamamlandi = 6,
    Kapandi = 7
}

public enum RequestPriority
{
    Dusuk = 0,
    Orta = 1,
    Yuksek = 2,
    Kritik = 3
}

public enum AssessmentResult
{
    Beklemede = 0,
    Uygun = 1,
    UygunDegil = 2,
    VarOlanYazilimaYonlendirildi = 3,
    YeniGelistirme = 4
}

public enum ProjectStatus
{
    Planlama = 0,
    Analiz = 1,
    Gelistirme = 2,
    Test = 3,
    YayinaHazir = 4,
    Tamamlandi = 5,
    Askida = 6
}

public enum ApprovalStatus
{
    Beklemede = 0,
    Onaylandi = 1,
    Reddedildi = 2
}
