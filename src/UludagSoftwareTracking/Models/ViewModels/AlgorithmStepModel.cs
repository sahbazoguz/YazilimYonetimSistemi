using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UludagSoftwareTracking.Models.ViewModels;

public enum AlgorithmStepType
{
    Normal,
    Decision
}

public class AlgorithmBranchModel
{
    [Required]
    [StringLength(40)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string TargetCode { get; set; } = string.Empty;
}

public class AlgorithmStepModel
{
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AlgorithmStepType Type { get; set; } = AlgorithmStepType.Normal;

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [StringLength(120)]
    public string? Role { get; set; }

    [StringLength(10)]
    public string? NextCode { get; set; }

    public List<AlgorithmBranchModel> Branches { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
