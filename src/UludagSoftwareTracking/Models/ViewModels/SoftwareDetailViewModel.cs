using System;
using System.Collections.Generic;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class SoftwareDetailViewModel
{
    public Software? Yazilim { get; init; }

    public IReadOnlyCollection<SoftwareManual> Kilavuzlar { get; init; } = Array.Empty<SoftwareManual>();
}
