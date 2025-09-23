using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Departments.AnyAsync())
        {
            var departments = new List<Department>
            {
                new() { Id = 1, Name = "Bilgi İşlem Daire Başkanlığı", Description = "Üniversite genelinde yazılım ve altyapı yönetiminden sorumlu." },
                new() { Id = 2, Name = "Mühendislik Fakültesi", Description = "Mühendislik fakültesi taleplerinin yönetimi." },
                new() { Id = 3, Name = "İktisadi ve İdari Bilimler Fakültesi", Description = "İİBF özel yazılım gereksinimleri." },
                new() { Id = 4, Name = "Rektörlük", Description = "Kurumsal yönetim ve raporlama ihtiyaçları." }
            };

            await context.Departments.AddRangeAsync(departments);
            await context.SaveChangesAsync();
        }

        if (!await context.UserProfiles.AnyAsync())
        {
            var admin = new UserProfile
            {
                Id = 1,
                UserName = "ULUDAG\\admin",
                FullName = "Sistem Yöneticisi",
                Email = "admin@uludag.edu.tr",
                Role = UserRole.Admin,
                DepartmentId = 1
            };

            var birimYetkilisi = new UserProfile
            {
                Id = 2,
                UserName = "ULUDAG\\birimyetkilisi",
                FullName = "Birim Yetkilisi",
                Email = "birim.yetkilisi@uludag.edu.tr",
                Role = UserRole.BirimYetkilisi,
                DepartmentId = 2
            };

            var birimKullanicisi = new UserProfile
            {
                Id = 3,
                UserName = "ULUDAG\\birimkullanici",
                FullName = "Birim Kullanıcısı",
                Email = "birim.kullanici@uludag.edu.tr",
                Role = UserRole.BirimKullanicisi,
                DepartmentId = 2
            };

            var itDegerlendirme = new UserProfile
            {
                Id = 4,
                UserName = "ULUDAG\\itdegerlendirme",
                FullName = "Bilgi İşlem Uzmanı",
                Email = "it.degerlendirme@uludag.edu.tr",
                Role = UserRole.BilgiIslemDegerlendirmeEkibi,
                DepartmentId = 1
            };

            var yazilimLideri = new UserProfile
            {
                Id = 5,
                UserName = "ULUDAG\\yazilimlider",
                FullName = "Yazılım Ekibi Lideri",
                Email = "yazilim.lider@uludag.edu.tr",
                Role = UserRole.YazilimEkibiLideri,
                DepartmentId = 1
            };

            var yazilimci = new UserProfile
            {
                Id = 6,
                UserName = "ULUDAG\\yazilimci1",
                FullName = "Yazılım Geliştirici",
                Email = "yazilimci@uludag.edu.tr",
                Role = UserRole.Yazilimci,
                DepartmentId = 1
            };

            await context.UserProfiles.AddRangeAsync(admin, birimYetkilisi, birimKullanicisi, itDegerlendirme, yazilimLideri, yazilimci);
            await context.SaveChangesAsync();
        }

        if (!await context.Softwares.AnyAsync())
        {
            var catalog = new List<Software>
            {
                new()
                {
                    Id = 1,
                    Name = "Akademik Personel Takip Sistemi",
                    Description = "Akademik personel izin ve mesai takibini sağlayan merkezi çözüm.",
                    DepartmentId = 1,
                    Category = "İnsan Kaynakları",
                    TechnologyStack = ".NET 9, SQL Server",
                    SupportContact = "bilgiislem@uludag.edu.tr",
                    WebsiteUrl = "https://yazilim.uludag.edu.tr/personel"
                },
                new()
                {
                    Id = 2,
                    Name = "Laboratuvar Rezervasyon Portalı",
                    Description = "Laboratuvar cihazlarının planlanması ve raporlanmasını kolaylaştırır.",
                    DepartmentId = 2,
                    Category = "Akademik",
                    TechnologyStack = "ASP.NET MVC, SignalR",
                    SupportContact = "labdestek@uludag.edu.tr",
                    WebsiteUrl = "https://lab.uludag.edu.tr"
                }
            };

            await context.Softwares.AddRangeAsync(catalog);
            await context.SaveChangesAsync();
        }

        if (!await context.SoftwareManuals.AnyAsync())
        {
            var manuals = new List<SoftwareManual>
            {
                new()
                {
                    Id = 1,
                    Title = "Akademik Personel Kullanım Kılavuzu",
                    ManualType = ManualType.KullanimKilavuzu,
                    FilePath = "/docs/personel_kilavuzu_v1.pdf",
                    Version = "1.0",
                    SoftwareId = 1,
                    UploadedByUserId = 3
                },
                new()
                {
                    Id = 2,
                    Title = "Laboratuvar Portalı Teknik Kılavuz",
                    ManualType = ManualType.TeknikKilavuz,
                    FilePath = "/docs/lab_portal_teknik_v1.pdf",
                    Version = "1.0",
                    SoftwareId = 2,
                    UploadedByUserId = 5
                }
            };

            await context.SoftwareManuals.AddRangeAsync(manuals);
            await context.SaveChangesAsync();
        }

        if (!await context.SoftwareRequests.AnyAsync())
        {
            var request = new SoftwareRequest
            {
                Id = 1,
                Title = "Staj Yönetim Sistemi",
                Description = "Fakülte öğrencilerinin staj süreçlerinin dijital yönetimi talep edilmektedir.",
                DepartmentId = 2,
                RequestedByUserId = 3,
                Priority = RequestPriority.Yuksek,
                Status = RequestStatus.OnayBekleniyor,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            await context.SoftwareRequests.AddAsync(request);
            await context.SaveChangesAsync();

            var approval = new RequestApproval
            {
                Id = 1,
                RequestId = request.Id,
                ApprovedByUserId = 2,
                Status = ApprovalStatus.Beklemede
            };

            await context.RequestApprovals.AddAsync(approval);
            await context.SaveChangesAsync();
        }

        if (!await context.Projects.AnyAsync())
        {
            var project = new Project
            {
                Id = 1,
                RequestId = 1,
                Name = "Staj Yönetim Sistemi Geliştirme",
                Description = "Onaylanan staj talepleri için yeni yazılım geliştirme projesi.",
                Status = ProjectStatus.Planlama,
                LeadUserId = 5,
                StartDate = DateTime.UtcNow
            };

            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();

            var assignment = new ProjectAssignment
            {
                Id = 1,
                ProjectId = project.Id,
                UserId = 6,
                AssignedRole = "Yazılımcı",
                CompletionPercent = 0
            };

            await context.ProjectAssignments.AddAsync(assignment);
            await context.SaveChangesAsync();
        }
    }
}
