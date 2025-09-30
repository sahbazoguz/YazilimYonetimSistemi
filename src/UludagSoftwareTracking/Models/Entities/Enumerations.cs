namespace UludagSoftwareTracking.Models.Entities;

public enum UserRole
{
    Misafir = 0,
    Ogrenci = 1,
    Personel = 2,
    BirimKullanicisi = 3,
    BirimYetkilisi = 4,
    DegerlendiriciBir = 5,
    DegerlendiriciIki = 6,
    DegerlendiriciUc = 7,
    DegerlendirmeBaskani = 8,
    Yazilimci = 9,
    Admin = 10
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
    BaskanOnayiBekliyor = 5,
    Gelistirmede = 6,
    Tamamlandi = 7,
    Kapandi = 8
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

public enum AssessmentStage
{
    DegerlendiriciBir = 0,
    DegerlendiriciIki = 1,
    DegerlendiriciUc = 2,
    BaskanOnayi = 3
}

public enum WorkflowStepType
{
    Normal = 0,
    KararNoktasi = 1
}
