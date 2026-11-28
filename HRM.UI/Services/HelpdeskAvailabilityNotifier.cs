using System;

namespace HRM.UI.Services;

/// <summary>Notifies all active layout components (Sidebar, GlobalHelpdeskWidget) across circuits when SuperAdmin toggles Helpdesk availability.</summary>
public sealed class HelpdeskAvailabilityNotifier
{
    public event Action? Changed;

    public void NotifyChanged()
    {
        var handlers = Changed;
        if (handlers == null) return;
        foreach (Action handler in handlers.GetInvocationList())
        {
            try { handler(); }
            catch { }
        }
    }
}
