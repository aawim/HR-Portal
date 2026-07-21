using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ConfigurableValue
{
    public int ConfigurableValueId { get; set; }

    public string? Value { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsSystemConfigValue { get; set; }

    public string? Comment { get; set; }

    public string Type { get; set; } = null!;
}
