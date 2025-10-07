using System;

namespace UludagSoftwareTracking.Models.ViewModels;

public class SelectableUserViewModel
{
    public int Id { get; init; }

    public string AdSoyad { get; init; } = string.Empty;

    public string Birim { get; init; } = string.Empty;
}
