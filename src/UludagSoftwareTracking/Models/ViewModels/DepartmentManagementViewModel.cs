using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class DepartmentManagementViewModel
{
    public IReadOnlyList<DepartmentManagementRowViewModel> Departments { get; set; } = Array.Empty<DepartmentManagementRowViewModel>();

    public DepartmentInputModel YeniBirim { get; set; } = new();

    public IReadOnlyList<UserAssignmentOptionViewModel> KullaniciSecenekleri { get; set; } =
        Array.Empty<UserAssignmentOptionViewModel>();
}

public class DepartmentManagementRowViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ContactEmail { get; set; }

    public string? PhoneNumber { get; set; }

    public IReadOnlyList<UserSummaryViewModel> Yetkililer { get; set; } = Array.Empty<UserSummaryViewModel>();

    public IReadOnlyList<UserSummaryViewModel> Kullanicilar { get; set; } = Array.Empty<UserSummaryViewModel>();
}

public class UserSummaryViewModel
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }
}

public class UserAssignmentOptionViewModel
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole CurrentRole { get; set; }

    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public string? Email { get; set; }
}

public class DepartmentInputModel
{
    [Required]
    [Display(Name = "Birim Adı")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [StringLength(400)]
    public string? Description { get; set; }

    [Display(Name = "E-posta")]
    [EmailAddress]
    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [Display(Name = "Telefon")]
    [StringLength(25)]
    public string? PhoneNumber { get; set; }
}

public class DepartmentUpdateInputModel : DepartmentInputModel
{
    [Required]
    public int Id { get; set; }
}

public class DepartmentAssignmentInputModel
{
    [Required]
    [Display(Name = "Birim")]
    public int DepartmentId { get; set; }

    [Required]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rol")]
    public UserRole Role { get; set; }
}
