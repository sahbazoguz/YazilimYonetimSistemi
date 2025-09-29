using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UludagSoftwareTracking.Models.ViewModels;

public class AlgorithmFlowModel
{
    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string StartCode { get; set; } = string.Empty;
}

public class AlgorithmDesignerModel
{
    public List<AlgorithmFlowModel> Flows { get; set; } = new();

    public List<AlgorithmStepModel> Steps { get; set; } = new();
}
