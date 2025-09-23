using System;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Services.Security;

namespace UludagSoftwareTracking.Extensions;

public static class UserRoleExtensions
{
    public static string ToDisplayName(this UserRole role) => role switch
    {
        UserRole.Misafir => RoleConstants.Roles.Misafir,
        UserRole.Ogrenci => RoleConstants.Roles.Ogrenci,
        UserRole.Personel => RoleConstants.Roles.Personel,
        UserRole.BirimKullanicisi => RoleConstants.Roles.BirimKullanicisi,
        UserRole.BirimYetkilisi => RoleConstants.Roles.BirimYetkilisi,
        UserRole.BilgiIslemDegerlendirmeEkibi => RoleConstants.Roles.BilgiIslemDegerlendirmeEkibi,
        UserRole.YazilimEkibiLideri => RoleConstants.Roles.YazilimEkibiLideri,
        UserRole.Yazilimci => RoleConstants.Roles.Yazilimci,
        UserRole.Admin => RoleConstants.Roles.Admin,
        _ => RoleConstants.Roles.Misafir
    };

    public static UserRole FromDisplayName(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return UserRole.Misafir;
        }

        return role switch
        {
            var r when string.Equals(r, RoleConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase) => UserRole.Admin,
            var r when string.Equals(r, RoleConstants.Roles.BirimYetkilisi, StringComparison.OrdinalIgnoreCase) => UserRole.BirimYetkilisi,
            var r when string.Equals(r, RoleConstants.Roles.BirimKullanicisi, StringComparison.OrdinalIgnoreCase) => UserRole.BirimKullanicisi,
            var r when string.Equals(r, RoleConstants.Roles.BilgiIslemDegerlendirmeEkibi, StringComparison.OrdinalIgnoreCase) => UserRole.BilgiIslemDegerlendirmeEkibi,
            var r when string.Equals(r, RoleConstants.Roles.YazilimEkibiLideri, StringComparison.OrdinalIgnoreCase) => UserRole.YazilimEkibiLideri,
            var r when string.Equals(r, RoleConstants.Roles.Yazilimci, StringComparison.OrdinalIgnoreCase) => UserRole.Yazilimci,
            var r when string.Equals(r, RoleConstants.Roles.Ogrenci, StringComparison.OrdinalIgnoreCase) => UserRole.Ogrenci,
            var r when string.Equals(r, RoleConstants.Roles.Personel, StringComparison.OrdinalIgnoreCase) => UserRole.Personel,
            _ => UserRole.Misafir
        };
    }
}
