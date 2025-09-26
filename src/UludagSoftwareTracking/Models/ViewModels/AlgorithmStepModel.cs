using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AlgorithmStepModel
{
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Type { get; set; } = "Normal";

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Role { get; set; }

    public JsonNode? Next { get; set; }
}
