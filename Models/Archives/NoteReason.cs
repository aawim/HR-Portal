using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class NoteReason
{
    public int NoteReasonId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}
