using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class DepartmentManagementViewModel
{
    public IReadOnlyList<DepartmentManagementRowViewModel> Departments { get; set; } = Array.Empty<DepartmentManagementRowViewModel>();

    public DepartmentInputModel YeniBirim { get; set; } = new();

    public DepartmentAssignmentInputModel Yetkilendirme { get; set; } = new();

    public IReadOnlyList<SelectListItem> KullaniciSecenekleri { get; set; } = Array.Empty<SelectListItem>();
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
