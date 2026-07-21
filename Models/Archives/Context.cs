using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Context
{
    public int ContextId { get; set; }

    public string Name { get; set; } = null!;

    public string? ContextKey { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public virtual ICollection<NavigationLink> NavigationLinks { get; set; } = new List<NavigationLink>();

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual ICollection<RequestTypeSpecificDocumentType> RequestTypeSpecificDocumentTypes { get; set; } = new List<RequestTypeSpecificDocumentType>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
}
