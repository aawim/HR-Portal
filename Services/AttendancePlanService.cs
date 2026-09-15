using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Services.Attendance.AttendancePlan;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class AttendancePlanService : IAttendancePlanService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IAttendanceLogDataLoader _loader;
         private readonly IPlanningProviderResolver _planningProviderResolver;
        private readonly IWorkPlanGenerator _workPlanGenerator;


        public AttendancePlanService(IDbContextFactory<HrmTeContext> factory, IAttendanceLogDataLoader loader, IUserAccessService userAccessService, IPlanningProviderResolver planningProviderResolver, IWorkPlanGenerator workPlanGenerator)
        {
            _dbFactory = factory;
            _loader = loader;
            _workPlanGenerator = workPlanGenerator;
            _planningProviderResolver = planningProviderResolver;
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
            var nextDate = date.AddDays(1);

            var plan =
                await db.WorkPlans
                    .AsNoTracking()
                    .Where(x =>
                        x.IndividualId == individualId &&
                        x.JobId == jobId &&
                        x.WorkDate >= date &&
                        x.WorkDate < nextDate &&
                        x.IsValid)
                    .OrderByDescending(x =>
                        x.IsManual)
                    .ThenByDescending(x =>
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

                            WorkPlanId =
                                x.WorkPlanId,

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
                WorkPlanId =
                    plan.WorkPlanId,

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
        //public async Task<AttendanceWorkPlanDto?> GetPlanAsync(
        //    int individualId,
        //    int jobId,
        //    DateTime workDate,
        //    CancellationToken cancellationToken = default)
        // {
        //     await using var db =
        //         await _dbFactory.CreateDbContextAsync(
        //             cancellationToken);

        //     var date = workDate.Date;
        //     var nextDate = date.AddDays(1);


        //     var plan =
        //         await db.WorkPlans
        //             .AsNoTracking()
        //             .Where(x =>
        //                 x.IndividualId == individualId &&
        //                 x.JobId == jobId &&
        //                 x.WorkDate.Date == date &&
        //                 x.IsValid)
        //             .OrderByDescending(x =>
        //                 x.Version)
        //             .ThenByDescending(x =>
        //                 x.CreatedDate)
        //             .Select(x => new
        //             {
        //                 x.WorkPlanId,
        //                 x.IndividualId,
        //                 x.JobId,

        //                 OrganisationId =
        //                     x.OrganisationBusinessEntityId,

        //                 x.WorkDate,
        //                 x.WorkTemplateId,
        //                 x.IsFinalized,
        //                 x.IsGenerated,
        //                 x.IsManual
        //             })
        //             .FirstOrDefaultAsync(
        //                 cancellationToken);

        //     if (plan == null)
        //         return null;

        //     var segments =
        //         await db.WorkPlanSegments
        //             .AsNoTracking()
        //             .Where(x =>
        //                 x.WorkPlanId == plan.WorkPlanId &&
        //                 x.IsValid)
        //             .OrderBy(x =>
        //                 x.SequenceNumber)
        //             .Select(x =>
        //                 new AttendanceWorkSegmentDto
        //                 {
        //                     WorkPlanSegmentId =
        //                         x.WorkPlanSegmentId,

        //                     WorkPlanId = x.WorkPlanId,

        //                     WorkTemplateSegmentId =
        //                         x.WorkTemplateSegmentId,

        //                     WorkSegmentTypeId =
        //                         x.WorkSegmentTypeId,

        //                     Name =
        //                         x.Name,

        //                     Description =
        //                         x.Description,

        //                     SequenceNumber =
        //                         x.SequenceNumber,

        //                     StartDateTime =
        //                         x.StartDateTime,

        //                     EndDateTime =
        //                         x.EndDateTime,

        //                     GraceBeforeMinutes =
        //                         x.GraceBeforeMinutes,

        //                     GraceAfterMinutes =
        //                         x.GraceAfterMinutes,

        //                     IsMandatory =
        //                         x.IsMandatory,

        //                     RequiresAttendance =
        //                         x.RequiresAttendance,

        //                     RequiresLocationValidation =
        //                         x.RequiresLocationValidation,

        //                     RequiresDeviceValidation =
        //                         x.RequiresDeviceValidation,

        //                     IsPaid =
        //                         x.IsPaid,

        //                     IsCompleted =
        //                         x.IsCompleted,

        //                     AttendanceId =
        //                         x.AttendanceId
        //                 })
        //             .ToListAsync(
        //                 cancellationToken);

        //     return new AttendanceWorkPlanDto
        //     {
        //         WorkPlanId = plan.WorkPlanId,

        //         IndividualId =
        //             plan.IndividualId,

        //         JobId =
        //             plan.JobId,

        //         OrganisationId =
        //             plan.OrganisationId,

        //         WorkDate =
        //             plan.WorkDate,

        //         WorkTemplateId =
        //             plan.WorkTemplateId,

        //         IsFinalized =
        //             plan.IsFinalized,

        //         IsGenerated =
        //             plan.IsGenerated,

        //         IsManual =
        //             plan.IsManual,

        //         Segments =
        //             segments
        //     };
        // }


        public async Task<AttendanceWorkPlanDto?> GetOrGenerateAsync(
            int individualId,
            int jobId,
            int organisationBusinessEntityId,
            DateTime attendanceTime,
            CancellationToken cancellationToken = default)
        {
            var workDate = DateOnly.FromDateTime(attendanceTime);

            var provider = await _planningProviderResolver.ResolveAsync(
                organisationBusinessEntityId,
                workDate,
                cancellationToken);

            if (provider.UsesWorkPlanning)
            {
                return await _workPlanGenerator.GenerateOrGetAsync(
                    individualId,
                    jobId,
                    organisationBusinessEntityId,
                    workDate,
                    cancellationToken);
            }

            if (provider.UsesLegacyShift)
            {
                // Use your existing GetPlanAsync database/legacy logic here.
                return await GetPlanAsync(
                    individualId,
                    jobId,
                    attendanceTime);
            }

            return null;
        }


 
    }
}
