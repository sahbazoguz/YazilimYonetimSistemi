using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AlgorithmStepModel
{
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
