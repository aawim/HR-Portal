using HRM.Models.AttendanceDeductions;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<AttendanceDeductionPolicy> AttendanceDeductionPolicies => Set<AttendanceDeductionPolicy>();
    public DbSet<AttendanceAdjustment> AttendanceAdjustments => Set<AttendanceAdjustment>();
    public DbSet<AttendanceExceptionReview> AttendanceExceptionReviews => Set<AttendanceExceptionReview>();
}
