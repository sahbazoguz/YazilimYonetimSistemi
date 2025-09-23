using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UludagSoftwareTracking.Data;
using UludagSoftwareTracking.Extensions;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;
using UludagSoftwareTracking.Services.Security;

namespace UludagSoftwareTracking.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(UserProfile? user, CancellationToken cancellationToken = default)
    {
        var role = user?.Role ?? UserRole.Misafir;
        var tiles = new List<DashboardTileViewModel>();
        var criticalRequests = Array.Empty<RequestListItemViewModel>();
        var activeProjects = Array.Empty<Project>();
        var featuredSoftwares = Array.Empty<Software>();
        var recentManuals = Array.Empty<SoftwareManual>();

        switch (role)
        {
            case UserRole.Misafir:
            case UserRole.Ogrenci:
            case UserRole.Personel:
                featuredSoftwares = await GetFeaturedSoftwaresAsync(cancellationToken);
                recentManuals = await GetLatestManualsAsync(cancellationToken);
                tiles.Add(new DashboardTileViewModel { Baslik = "Yayındaki Yazılımlar", Deger = featuredSoftwares.Length.ToString(), Stil = "primary" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Toplam Kılavuz", Deger = recentManuals.Length.ToString(), Stil = "info" });
                break;
            case UserRole.BirimKullanicisi:
                var myRequests = await _context.SoftwareRequests
                    .Where(r => r.RequestedByUserId == user!.Id)
                    .Include(r => r.Department)
                    .OrderByDescending(r => r.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                tiles.Add(new DashboardTileViewModel { Baslik = "Toplam Talep", Deger = myRequests.Count.ToString(), Stil = "primary" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Onay Bekleyen", Deger = myRequests.Count(r => r.Status == RequestStatus.OnayBekleniyor).ToString(), Stil = "warning" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Değerlendirmede", Deger = myRequests.Count(r => r.Status == RequestStatus.Degerlendirmede).ToString(), Stil = "info" });
                criticalRequests = myRequests
                    .Where(r => r.Priority is RequestPriority.Yuksek or RequestPriority.Kritik)
                    .Select(r => new RequestListItemViewModel
                    {
                        Id = r.Id,
                        Baslik = r.Title,
                        Durum = r.Status.ToString(),
                        BirimAdi = r.Department?.Name ?? string.Empty,
                        TalepSahibi = user.FullName,
                        Oncelik = r.Priority,
                        OlusturmaTarihi = r.CreatedAt
                    })
                    .ToArray();
                recentManuals = await GetLatestManualsAsync(cancellationToken);
                break;
            case UserRole.BirimYetkilisi:
                var departmentRequests = await _context.SoftwareRequests
                    .Where(r => r.DepartmentId == user!.DepartmentId)
                    .Include(r => r.Department)
                    .Include(r => r.RequestedByUser)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                tiles.Add(new DashboardTileViewModel { Baslik = "Birim Talepleri", Deger = departmentRequests.Count.ToString(), Stil = "primary" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Onay Bekleyen", Deger = departmentRequests.Count(r => r.Status == RequestStatus.OnayBekleniyor).ToString(), Stil = "warning" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Devam Eden", Deger = departmentRequests.Count(r => r.Status == RequestStatus.Gelistirmede).ToString(), Stil = "success" });

                criticalRequests = departmentRequests
                    .Where(r => r.Status == RequestStatus.OnayBekleniyor)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new RequestListItemViewModel
                    {
                        Id = r.Id,
                        Baslik = r.Title,
                        Durum = "Onay Bekliyor",
                        BirimAdi = r.Department?.Name ?? string.Empty,
                        TalepSahibi = r.RequestedByUser?.FullName ?? r.RequestedByUser?.UserName ?? string.Empty,
                        Oncelik = r.Priority,
                        OlusturmaTarihi = r.CreatedAt
                    })
                    .ToArray();
                break;
            case UserRole.BilgiIslemDegerlendirmeEkibi:
                var pendingAssessments = await _context.SoftwareRequests
                    .Where(r => r.Status == RequestStatus.Degerlendirmede)
                    .Include(r => r.Department)
                    .Include(r => r.RequestedByUser)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                tiles.Add(new DashboardTileViewModel { Baslik = "Bekleyen Değerlendirme", Deger = pendingAssessments.Count.ToString(), Stil = "warning" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Bu Ay Gelen", Deger = pendingAssessments.Count(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToString(), Stil = "info" });
                criticalRequests = pendingAssessments
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new RequestListItemViewModel
                    {
                        Id = r.Id,
                        Baslik = r.Title,
                        Durum = "Değerlendirmede",
                        BirimAdi = r.Department?.Name ?? string.Empty,
                        TalepSahibi = r.RequestedByUser?.FullName ?? r.RequestedByUser?.UserName ?? string.Empty,
                        Oncelik = r.Priority,
                        OlusturmaTarihi = r.CreatedAt
                    })
                    .ToArray();
                break;
            case UserRole.YazilimEkibiLideri:
            case UserRole.Yazilimci:
                activeProjects = await _context.Projects
                    .Where(p => p.Status == ProjectStatus.Planlama || p.Status == ProjectStatus.Gelistirme || p.Status == ProjectStatus.Test)
                    .Include(p => p.Request)
                    .ThenInclude(r => r.Department)
                    .Include(p => p.Assignments)
                    .ThenInclude(a => a.User)
                    .AsNoTracking()
                    .ToArrayAsync(cancellationToken);

                tiles.Add(new DashboardTileViewModel { Baslik = "Aktif Proje", Deger = activeProjects.Length.ToString(), Stil = "success" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Atandığım Görev", Deger = activeProjects.Sum(p => p.Assignments.Count(a => a.UserId == user!.Id)).ToString(), Stil = "primary" });
                criticalRequests = activeProjects
                    .Select(p => new RequestListItemViewModel
                    {
                        Id = p.RequestId,
                        Baslik = p.Name,
                        Durum = p.Status.ToString(),
                        BirimAdi = p.Request?.Department?.Name ?? string.Empty,
                        TalepSahibi = p.Request?.RequestedByUser?.FullName ?? string.Empty,
                        Oncelik = p.Request?.Priority ?? RequestPriority.Orta,
                        OlusturmaTarihi = p.Request?.CreatedAt ?? DateTime.UtcNow
                    })
                    .ToArray();
                break;
            case UserRole.Admin:
                var totalUsers = await _context.UserProfiles.CountAsync(cancellationToken);
                var totalRequests = await _context.SoftwareRequests.CountAsync(cancellationToken);
                var completedRequests = await _context.SoftwareRequests.CountAsync(r => r.Status == RequestStatus.Tamamlandi || r.Status == RequestStatus.Kapandi, cancellationToken);

                tiles.Add(new DashboardTileViewModel { Baslik = "Kullanıcı Sayısı", Deger = totalUsers.ToString(), Stil = "primary" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Toplam Talep", Deger = totalRequests.ToString(), Stil = "info" });
                tiles.Add(new DashboardTileViewModel { Baslik = "Tamamlanan", Deger = completedRequests.ToString(), Stil = "success" });

                featuredSoftwares = await GetFeaturedSoftwaresAsync(cancellationToken);
                recentManuals = await GetLatestManualsAsync(cancellationToken);
                break;
        }

        return new DashboardViewModel
        {
            KullaniciAdi = user?.FullName ?? "Ziyaretçi",
            Rol = user?.Role.ToDisplayName() ?? RoleConstants.Roles.Misafir,
            Kutucuklar = tiles,
            KritikTalepler = criticalRequests,
            AktifProjeler = activeProjects,
            OneCikanYazilimlar = featuredSoftwares,
            SonKilavuzlar = recentManuals
        };
    }

    private async Task<Software[]> GetFeaturedSoftwaresAsync(CancellationToken cancellationToken)
    {
        return await _context.Softwares
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Take(8)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }

    private async Task<SoftwareManual[]> GetLatestManualsAsync(CancellationToken cancellationToken)
    {
        return await _context.SoftwareManuals
            .Include(m => m.Software)
            .OrderByDescending(m => m.UploadedOn)
            .Take(8)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }
}
