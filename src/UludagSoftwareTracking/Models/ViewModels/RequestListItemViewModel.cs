using System;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class RequestListItemViewModel
{
    public int Id { get; init; }

    public string Baslik { get; init; } = string.Empty;

    public string Durum { get; init; } = string.Empty;

    public string BirimAdi { get; init; } = string.Empty;

    public string TalepSahibi { get; init; } = string.Empty;

    public RequestPriority Oncelik { get; init; }

    public DateTime OlusturmaTarihi { get; init; }
}
