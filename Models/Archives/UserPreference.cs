using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class UserPreference
{
    public int UserPreferenceId { get; set; }

    public int? SelectedLanguageId { get; set; }

    public int OperationLogId { get; set; }

    public int? SelectedContextId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Context? SelectedContext { get; set; }

    public virtual Language? SelectedLanguage { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
