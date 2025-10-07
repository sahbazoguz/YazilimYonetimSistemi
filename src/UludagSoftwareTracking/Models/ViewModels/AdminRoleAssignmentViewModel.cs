using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AdminRoleAssignmentViewModel
{
    [Required]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rol")]
    public UserRole Role { get; set; }

    [Display(Name = "Birim")]
    public int? DepartmentId { get; set; }

    public IReadOnlyCollection<Department> Departments { get; set; } = Array.Empty<Department>();

    public IReadOnlyCollection<UserProfile> KullaniciListesi { get; set; } = Array.Empty<UserProfile>();
}
