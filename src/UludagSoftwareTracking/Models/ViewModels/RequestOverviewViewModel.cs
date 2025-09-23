using System;
using System.Collections.Generic;

namespace UludagSoftwareTracking.Models.ViewModels;

public class RequestOverviewViewModel
{
    public string Baslik { get; init; } = string.Empty;

    public IReadOnlyCollection<RequestListItemViewModel> Talepler { get; init; } = Array.Empty<RequestListItemViewModel>();
}
