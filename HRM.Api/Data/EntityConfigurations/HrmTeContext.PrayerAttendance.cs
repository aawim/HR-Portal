using HRM.Models.PrayerAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<AttendanceSchedulePolicy> AttendanceSchedulePolicies => Set<AttendanceSchedulePolicy>();
    public DbSet<EmployeeAttendancePolicyAssignment> EmployeeAttendancePolicyAssignments => Set<EmployeeAttendancePolicyAssignment>();
    public DbSet<Mosque> Mosques => Set<Mosque>();
    public DbSet<PrayerTimetableVersion> PrayerTimetableVersions => Set<PrayerTimetableVersion>();
    public DbSet<PrayerTimetableEntry> PrayerTimetableEntries => Set<PrayerTimetableEntry>();
    public DbSet<AttendanceRuleSet> AttendanceRuleSets => Set<AttendanceRuleSet>();
    public DbSet<PrayerDutyAssignment> PrayerDutyAssignments => Set<PrayerDutyAssignment>();
    public DbSet<PrayerDutyReplacement> PrayerDutyReplacements => Set<PrayerDutyReplacement>();
    public DbSet<AttendanceSessionResult> AttendanceSessionResults => Set<AttendanceSessionResult>();
    public DbSet<AttendanceEventMatch> AttendanceEventMatches => Set<AttendanceEventMatch>();
}
