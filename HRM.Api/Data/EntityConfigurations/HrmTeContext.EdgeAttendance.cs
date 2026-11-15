using HRM.Models.EdgeAttendance;
using Microsoft.EntityFrameworkCore;
namespace HRM.Models;
public partial class HrmTeContext { public DbSet<EdgeAttendanceAgent> EdgeAttendanceAgents=>Set<EdgeAttendanceAgent>(); public DbSet<EdgeAttendanceTrustedIssuer> EdgeAttendanceTrustedIssuers=>Set<EdgeAttendanceTrustedIssuer>(); public DbSet<EdgeAttendanceAgentDevice> EdgeAttendanceAgentDevices=>Set<EdgeAttendanceAgentDevice>(); public DbSet<EdgeAttendanceEventReceipt> EdgeAttendanceEventReceipts=>Set<EdgeAttendanceEventReceipt>(); public DbSet<EdgeAttendanceIngestionFailure> EdgeAttendanceIngestionFailures=>Set<EdgeAttendanceIngestionFailure>(); }
