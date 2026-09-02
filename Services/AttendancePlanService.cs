using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Services.Attendance.AttendancePlan;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class AttendancePlanService : IAttendancePlanService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IAttendanceLogDataLoader _loader;
        private readonly IUserAccessService _userAccessService;


        public AttendancePlanService(IDbContextFactory<HrmTeContext> factory, IAttendanceLogDataLoader loader, IUserAccessService userAccessService)
        {
            _dbFactory = factory;
            _loader = loader;
            _userAccessService = userAccessService;
        }


        public async Task<AttendanceWorkPlanDto?> GetPlanAsync(
       int individualId,
       int jobId,
       DateTime workDate,
       CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var date = workDate.Date;

            var plan =
                await db.WorkPlans
                    .AsNoTracking()
                    .Where(x =>
                        x.IndividualId == individualId &&
                        x.JobId == jobId &&
                        x.WorkDate.Date == date &&
                        x.IsValid)
                    .OrderByDescending(x =>
                        x.Version)
                    .ThenByDescending(x =>
                        x.CreatedDate)
                    .Select(x => new
                    {
                        x.WorkPlanId,
                        x.IndividualId,
                        x.JobId,

                        OrganisationId =
                            x.OrganisationBusinessEntityId,

                        x.WorkDate,
                        x.WorkTemplateId,
                        x.IsFinalized,
                        x.IsGenerated,
                        x.IsManual
                    })
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (plan == null)
                return null;

            var segments =
                await db.WorkPlanSegments
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkPlanId == plan.WorkPlanId &&
                        x.IsValid)
                    .OrderBy(x =>
                        x.SequenceNumber)
                    .Select(x =>
                        new AttendanceWorkSegmentDto
                        {
                            WorkPlanSegmentId =
                                x.WorkPlanSegmentId,

                            WorkPlanId = x.WorkPlanId,

                            WorkTemplateSegmentId =
                                x.WorkTemplateSegmentId,

                            WorkSegmentTypeId =
                                x.WorkSegmentTypeId,

                            Name =
                                x.Name,

                            Description =
                                x.Description,

                            SequenceNumber =
                                x.SequenceNumber,

                            StartDateTime =
                                x.StartDateTime,

                            EndDateTime =
                                x.EndDateTime,

                            GraceBeforeMinutes =
                                x.GraceBeforeMinutes,

                            GraceAfterMinutes =
                                x.GraceAfterMinutes,

                            IsMandatory =
                                x.IsMandatory,

                            RequiresAttendance =
                                x.RequiresAttendance,

                            RequiresLocationValidation =
                                x.RequiresLocationValidation,

                            RequiresDeviceValidation =
                                x.RequiresDeviceValidation,

                            IsPaid =
                                x.IsPaid,

                            IsCompleted =
                                x.IsCompleted,

                            AttendanceId =
                                x.AttendanceId
                        })
                    .ToListAsync(
                        cancellationToken);

            return new AttendanceWorkPlanDto
            {
                WorkPlanId = plan.WorkPlanId,

                IndividualId =
                    plan.IndividualId,

                JobId =
                    plan.JobId,

                OrganisationId =
                    plan.OrganisationId,

                WorkDate =
                    plan.WorkDate,

                WorkTemplateId =
                    plan.WorkTemplateId,

                IsFinalized =
                    plan.IsFinalized,

                IsGenerated =
                    plan.IsGenerated,

                IsManual =
                    plan.IsManual,

                Segments =
                    segments
            };
        }
    }
}
