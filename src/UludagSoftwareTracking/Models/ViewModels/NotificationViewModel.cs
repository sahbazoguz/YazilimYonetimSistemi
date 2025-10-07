using System;

namespace UludagSoftwareTracking.Models.ViewModels;

public class NotificationViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Link { get; init; }

    public DateTime CreatedAt { get; init; }
}
