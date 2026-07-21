using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Note
{
    public int NoteId { get; set; }

    public string Text { get; set; } = null!;

    public int NoteReasonId { get; set; }

    public int OperationLogId { get; set; }

    public virtual NoteReason NoteReason { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
