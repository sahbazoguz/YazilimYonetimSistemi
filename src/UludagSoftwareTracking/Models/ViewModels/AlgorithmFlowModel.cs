using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AlgorithmFlowModel
{
    [StringLength(100)]
    public string? Label { get; set; }

    [Required]
    [StringLength(50)]
    public string StartCode { get; set; } = string.Empty;
}

public class AlgorithmDesignerModel
{
    public List<AlgorithmFlowModel> Flows { get; set; } = new();

    public List<AlgorithmStepModel> Steps { get; set; } = new();
}
