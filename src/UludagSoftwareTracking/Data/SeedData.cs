using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        const string bilgiIslemDepartmentName = "Bilgi İşlem Daire Başkanlığı";
        const string muhendislikDepartmentName = "Mühendislik Fakültesi";
        const string iibfDepartmentName = "İktisadi ve İdari Bilimler Fakültesi";
        const string rektorlukDepartmentName = "Rektörlük";

        var departmentNames = new[]
        {
            bilgiIslemDepartmentName,
            muhendislikDepartmentName,
            iibfDepartmentName,
            rektorlukDepartmentName
        };

        if (!await context.Departments.AnyAsync())
        {
            var departments = new List<Department>
            {
                new()
                {
                    Name = bilgiIslemDepartmentName,
                    Description = "Üniversite genelinde yazılım ve altyapı yönetiminden sorumlu."
                },
                new()
                {
                    Name = muhendislikDepartmentName,
                    Description = "Mühendislik fakültesi taleplerinin yönetimi."
                },
                new()
                {
                    Name = iibfDepartmentName,
                    Description = "İİBF özel yazılım gereksinimleri."
                },
                new()
                {
                    Name = rektorlukDepartmentName,
                    Description = "Kurumsal yönetim ve raporlama ihtiyaçları."
                }
            };

            await context.Departments.AddRangeAsync(departments);
            await context.SaveChangesAsync();
        }

        var departmentLookup = await context.Departments
            .Where(d => departmentNames.Contains(d.Name))
            .ToDictionaryAsync(d => d.Name, d => d.Id);

        const string adminUserName = "ULUDAG\\admin";
        const string birimYetkilisiUserName = "ULUDAG\\birimyetkilisi";
        const string birimKullanicisiUserName = "ULUDAG\\birimkullanici";
        const string itDegerlendirmeUserName = "ULUDAG\\itdegerlendirme";
        const string yazilimLideriUserName = "ULUDAG\\yazilimlider";
        const string yazilimciUserName = "ULUDAG\\yazilimci1";

        var defaultUserNames = new[]
        {
            adminUserName,
            birimYetkilisiUserName,
            birimKullanicisiUserName,
            itDegerlendirmeUserName,
            yazilimLideriUserName,
            yazilimciUserName
        };

        if (!await context.UserProfiles.AnyAsync())
        {
            const string defaultPasswordHash = "100000.KV70gYKE5YwR2szBa49zcA==.nEc4jc4/Z0RfNrf4SZTcHT/iANede0gSl1fFcCc8uBg=";

            var admin = new UserProfile
            {
                UserName = adminUserName,
                FullName = "Sistem Yöneticisi",
                Email = "admin@uludag.edu.tr",
                PasswordHash = defaultPasswordHash,
                Role = UserRole.Admin,
                DepartmentId = departmentLookup[bilgiIslemDepartmentName]
            };

            var birimYetkilisi = new UserProfile
            {
                UserName = birimYetkilisiUserName,
                FullName = "Birim Yetkilisi",
                Email = "birim.yetkilisi@uludag.edu.tr",
                PasswordHash = defaultPasswordHash,
                Role = UserRole.BirimYetkilisi,
                DepartmentId = departmentLookup[muhendislikDepartmentName]
            };

            var birimKullanicisi = new UserProfile
            {
                UserName = birimKullanicisiUserName,
                FullName = "Birim Kullanıcısı",
                Email = "birim.kullanici@uludag.edu.tr",
                PasswordHash = defaultPasswordHash,
                Role = UserRole.BirimKullanicisi,
                DepartmentId = departmentLookup[muhendislikDepartmentName]
            };

            var itDegerlendirme = new UserProfile
            {
                UserName = itDegerlendirmeUserName,
                FullName = "Bilgi İşlem Uzmanı",
                Email = "it.degerlendirme@uludag.edu.tr",
                PasswordHash = defaultPasswordHash,
                Role = UserRole.BilgiIslemDegerlendirmeEkibi,
                DepartmentId = departmentLookup[bilgiIslemDepartmentName]
            };

            var yazilimLideri = new UserProfile
            {
                UserName = yazilimLideriUserName,
                FullName = "Yazılım Ekibi Lideri",
                Email = "yazilim.lider@uludag.edu.tr",
                PasswordHash = defaultPasswordHash,
                Role = UserRole.YazilimEkibiLideri,
                DepartmentId = departmentLookup[bilgiIslemDepartmentName]
            };

            var yazilimci = new UserProfile
            {
                UserName = yazilimciUserName,
                FullName = "Yazılım Geliştirici",
                Email = "yazilimci@uludag.edu.tr",
                PasswordHash = defaultPasswordHash,
                Role = UserRole.Yazilimci,
                DepartmentId = departmentLookup[bilgiIslemDepartmentName]
            };

            await context.UserProfiles.AddRangeAsync(admin, birimYetkilisi, birimKullanicisi, itDegerlendirme, yazilimLideri, yazilimci);
            await context.SaveChangesAsync();
        }

        var userLookup = await context.UserProfiles
            .Where(u => defaultUserNames.Contains(u.UserName))
            .ToDictionaryAsync(u => u.UserName, u => u.Id);

        const string akademikPersonelSoftwareName = "Akademik Personel Takip Sistemi";
        const string laboratuvarPortalSoftwareName = "Laboratuvar Rezervasyon Portalı";

        var softwareNames = new[]
        {
            akademikPersonelSoftwareName,
            laboratuvarPortalSoftwareName
        };

        if (!await context.Softwares.AnyAsync())
        {
            var catalog = new List<Software>
            {
                new()
                {
                    Name = akademikPersonelSoftwareName,
                    Description = "Akademik personel izin ve mesai takibini sağlayan merkezi çözüm.",
                    DepartmentId = departmentLookup[bilgiIslemDepartmentName],
                    Category = "İnsan Kaynakları",
                    TechnologyStack = ".NET 8, SQL Server",
                    SupportContact = "bilgiislem@uludag.edu.tr",
                    WebsiteUrl = "https://yazilim.uludag.edu.tr/personel"
                },
                new()
                {
                    Name = laboratuvarPortalSoftwareName,
                    Description = "Laboratuvar cihazlarının planlanması ve raporlanmasını kolaylaştırır.",
                    DepartmentId = departmentLookup[muhendislikDepartmentName],
                    Category = "Akademik",
                    TechnologyStack = "ASP.NET MVC, SignalR",
                    SupportContact = "labdestek@uludag.edu.tr",
                    WebsiteUrl = "https://lab.uludag.edu.tr"
                }
            };

            await context.Softwares.AddRangeAsync(catalog);
            await context.SaveChangesAsync();
        }

        var softwareLookup = await context.Softwares
            .Where(s => softwareNames.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, s => s.Id);

        if (!await context.SoftwareManuals.AnyAsync())
        {
            var manuals = new List<SoftwareManual>
            {
                new()
                {
                    Title = "Akademik Personel Kullanım Kılavuzu",
                    ManualType = ManualType.KullanimKilavuzu,
                    FilePath = "/docs/personel_kilavuzu_v1.pdf",
                    Version = "1.0",
                    SoftwareId = softwareLookup[akademikPersonelSoftwareName],
                    UploadedByUserId = userLookup[birimKullanicisiUserName]
                },
                new()
                {
                    Title = "Laboratuvar Portalı Teknik Kılavuz",
                    ManualType = ManualType.TeknikKilavuz,
                    FilePath = "/docs/lab_portal_teknik_v1.pdf",
                    Version = "1.0",
                    SoftwareId = softwareLookup[laboratuvarPortalSoftwareName],
                    UploadedByUserId = userLookup[yazilimLideriUserName]
                }
            };

            await context.SoftwareManuals.AddRangeAsync(manuals);
            await context.SaveChangesAsync();
        }

        if (!await context.SoftwareRequests.AnyAsync())
        {
            var request = new SoftwareRequest
            {
                Title = "Staj Yönetim Sistemi",
                Description = "Fakülte öğrencilerinin staj süreçlerinin dijital yönetimi talep edilmektedir.",
                DepartmentId = departmentLookup[muhendislikDepartmentName],
                RequestedByUserId = userLookup[birimKullanicisiUserName],
                Priority = RequestPriority.Yuksek,
                Status = RequestStatus.OnayBekleniyor,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            await context.SoftwareRequests.AddAsync(request);
            await context.SaveChangesAsync();

            var approval = new RequestApproval
            {
                RequestId = request.Id,
                ApprovedByUserId = userLookup[birimYetkilisiUserName],
                Status = ApprovalStatus.Beklemede
            };

            await context.RequestApprovals.AddAsync(approval);
            await context.SaveChangesAsync();
        }

        var stajYonetimRequestId = await context.SoftwareRequests
            .Where(r => r.Title == "Staj Yönetim Sistemi")
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();

        if (!await context.Projects.AnyAsync())
        {
            if (stajYonetimRequestId.HasValue)
            {
                var project = new Project
                {
                    RequestId = stajYonetimRequestId.Value,
                    Name = "Staj Yönetim Sistemi Geliştirme",
                    Description = "Onaylanan staj talepleri için yeni yazılım geliştirme projesi.",
                    Status = ProjectStatus.Planlama,
                    LeadUserId = userLookup[yazilimLideriUserName],
                    StartDate = DateTime.UtcNow
                };

                await context.Projects.AddAsync(project);
                await context.SaveChangesAsync();

                var assignment = new ProjectAssignment
                {
                    ProjectId = project.Id,
                    UserId = userLookup[yazilimciUserName],
                    AssignedRole = "Yazılımcı",
                    CompletionPercent = 0
                };

                await context.ProjectAssignments.AddAsync(assignment);
                await context.SaveChangesAsync();
            }
        }
    }
}
