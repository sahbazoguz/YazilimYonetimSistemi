using System;
using System.Collections.Generic;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Models.ViewModels;

public class RequestDetailViewModel
{
    public SoftwareRequest? Talep { get; init; }

    public IReadOnlyCollection<RequestApproval> Onaylar { get; init; } = Array.Empty<RequestApproval>();

    public IReadOnlyCollection<RequestAssessment> Degerlendirmeler { get; init; } = Array.Empty<RequestAssessment>();

    public Project? Proje { get; init; }

    public IReadOnlyCollection<RequestDiscussionMessage> Mesajlar { get; init; } = Array.Empty<RequestDiscussionMessage>();

    public string AlgorithmJson { get; init; } = "{\"flows\":[],\"steps\":[]}";
}
