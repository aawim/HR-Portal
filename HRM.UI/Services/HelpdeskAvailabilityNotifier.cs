using System;

namespace HRM.UI.Services;

/// <summary>Notifies active layout components (Sidebar, GlobalHelpdeskWidget) when SuperAdmin toggles Helpdesk availability.</summary>
public sealed class HelpdeskAvailabilityNotifier
{
    public event Action? Changed;
    public void NotifyChanged() => Changed?.Invoke();
}
