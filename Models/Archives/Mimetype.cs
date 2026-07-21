using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Mimetype
{
    public string Extension { get; set; } = null!;

    public string Mimetype1 { get; set; } = null!;

    public bool? IsImage { get; set; }
}
