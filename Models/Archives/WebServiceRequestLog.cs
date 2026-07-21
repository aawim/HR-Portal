using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class WebServiceRequestLog
{
    public int RequestLogId { get; set; }

    public string? Controller { get; set; }

    public string? Action { get; set; }

    public string? RequestString { get; set; }

    public DateTime? DateTime { get; set; }

    public string? Ip { get; set; }

    public string? RequestHeaders { get; set; }
}
