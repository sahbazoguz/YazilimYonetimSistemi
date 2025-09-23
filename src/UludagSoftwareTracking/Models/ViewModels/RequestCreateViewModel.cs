using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class RequestCreateViewModel
{
    [Required(ErrorMessage = "Lütfen başlık giriniz")]
    [Display(Name = "Talep Başlığı")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lütfen talep açıklamasını giriniz")]
    [Display(Name = "Talep Açıklaması")]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Öncelik")]
    public RequestPriority Priority { get; set; } = RequestPriority.Orta;

    [Display(Name = "Hedef Tarih")]
    [DataType(DataType.Date)]
    public DateTime? DesiredCompletionDate { get; set; }

    [Required]
    [Display(Name = "Birim")]
    public int DepartmentId { get; set; }

    public IReadOnlyCollection<Department> Departments { get; set; } = Array.Empty<Department>();
}
