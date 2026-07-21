using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Language
{
    public int LanguageId { get; set; }

    public string Name { get; set; } = null!;

    public string NameDhivehi { get; set; } = null!;

    public string CultureName { get; set; } = null!;

    public virtual ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
}
