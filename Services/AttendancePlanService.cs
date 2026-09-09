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
            var nextDate = date.AddDays(1);


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



 


        //    public async Task<AttendanceWorkPlanDto?> GetPlanAsync(
        //int individualId,
        //int jobId,
        //DateTime workDate,
        //CancellationToken cancellationToken = default)
        //    {
        //        await using var db =
        //            await _dbFactory.CreateDbContextAsync(cancellationToken);

        //        var date = workDate.Date;

        //        Console.WriteLine("====================================");
        //        Console.WriteLine("ATTENDANCE PLAN DEBUG");
        //        Console.WriteLine($"IndividualID : {individualId}");
        //        Console.WriteLine($"JobID        : {jobId}");
        //        Console.WriteLine($"WorkDate     : {date:yyyy-MM-dd}");
        //        Console.WriteLine("====================================");

        //        //
        //        // First check ANY plans for the individual
        //        //
        //        var allPlansForIndividual =
        //            await db.WorkPlans
        //                .AsNoTracking()
        //                .Where(x =>
        //                    x.IndividualId == individualId)
        //                .Select(x => new
        //                {
        //                    x.WorkPlanId,
        //                    x.IndividualId,
        //                    x.JobId,
        //                    x.WorkDate,
        //                    x.WorkTemplateId,
        //                    x.IsValid
        //                })
        //                .OrderByDescending(x => x.WorkDate)
        //                .Take(10)
        //                .ToListAsync(cancellationToken);

        //        Console.WriteLine(
        //            $"Plans for individual: {allPlansForIndividual.Count}");

        //        foreach (var p in allPlansForIndividual)
        //        {
        //            Console.WriteLine(
        //                $"Plan={p.WorkPlanId}, " +
        //                $"Individual={p.IndividualId}, " +
        //                $"Job={p.JobId}, " +
        //                $"Date={p.WorkDate:yyyy-MM-dd}, " +
        //                $"Template={p.WorkTemplateId}, " +
        //                $"Valid={p.IsValid}");
        //        }

        //        //
        //        // Now find the actual required plan.
        //        //
        //        var plan =
        //            await db.WorkPlans
        //                .AsNoTracking()
        //                .Where(x =>
        //                    x.IndividualId == individualId &&
        //                    x.JobId == jobId &&
        //                    x.WorkDate >= date &&
        //                    x.WorkDate < date.AddDays(1) &&
        //                    x.IsValid)
        //                .OrderByDescending(x => x.Version)
        //                .ThenByDescending(x => x.CreatedDate)
        //                .FirstOrDefaultAsync(cancellationToken);

        //        if (plan == null)
        //        {
        //            Console.WriteLine(
        //                "NO WORK PLAN FOUND FOR PROVIDED FILTER.");

        //            return null;
        //        }

        //        Console.WriteLine(
        //            $"WORK PLAN FOUND: {plan.WorkPlanId}");

        //        //
        //        // Check segments without additional filters first.
        //        //
        //        var allSegments =
        //            await db.WorkPlanSegments
        //                .AsNoTracking()
        //                .Where(x =>
        //                    x.WorkPlanId == plan.WorkPlanId)
        //                .OrderBy(x => x.SequenceNumber)
        //                .ToListAsync(cancellationToken);

        //        Console.WriteLine(
        //            $"Segments found: {allSegments.Count}");

        //        foreach (var s in allSegments)
        //        {
        //            Console.WriteLine(
        //                $"Segment={s.WorkPlanSegmentId}, " +
        //                $"Name={s.Name}, " +
        //                $"Start={s.StartDateTime:yyyy-MM-dd HH:mm:ss}, " +
        //                $"End={s.EndDateTime:yyyy-MM-dd HH:mm:ss}, " +
        //                $"RequiresAttendance={s.RequiresAttendance}, " +
        //                $"Valid={s.IsValid}");
        //        }

        //        var segments =
        //            allSegments
        //                .Where(x => x.IsValid)
        //                .Select(x =>
        //                    new AttendanceWorkSegmentDto
        //                    {
        //                        WorkPlanSegmentId =
        //                            x.WorkPlanSegmentId,

        //                        WorkPlanId =
        //                            x.WorkPlanId,

        //                        WorkTemplateSegmentId =
        //                            x.WorkTemplateSegmentId,

        //                        WorkSegmentTypeId =
        //                            x.WorkSegmentTypeId,

        //                        Name =
        //                            x.Name,

        //                        Description =
        //                            x.Description,

        //                        SequenceNumber =
        //                            x.SequenceNumber,

        //                        StartDateTime =
        //                            x.StartDateTime,

        //                        EndDateTime =
        //                            x.EndDateTime,

        //                        GraceBeforeMinutes =
        //                            x.GraceBeforeMinutes,

        //                        GraceAfterMinutes =
        //                            x.GraceAfterMinutes,

        //                        IsMandatory =
        //                            x.IsMandatory,

        //                        RequiresAttendance =
        //                            x.RequiresAttendance,

        //                        RequiresLocationValidation =
        //                            x.RequiresLocationValidation,

        //                        RequiresDeviceValidation =
        //                            x.RequiresDeviceValidation,

        //                        IsPaid =
        //                            x.IsPaid,

        //                        IsCompleted =
        //                            x.IsCompleted,

        //                        AttendanceId =
        //                            x.AttendanceId
        //                    })
        //                .ToList();

        //        return new AttendanceWorkPlanDto
        //        {
        //            WorkPlanId =
        //                plan.WorkPlanId,

        //            IndividualId =
        //                plan.IndividualId,

        //            JobId =
        //                plan.JobId,

        //            OrganisationId =
        //                plan.OrganisationBusinessEntityId,

        //            WorkDate =
        //                plan.WorkDate,

        //            WorkTemplateId =
        //                plan.WorkTemplateId,

        //            IsFinalized =
        //                plan.IsFinalized,

        //            IsGenerated =
        //                plan.IsGenerated,

        //            IsManual =
        //                plan.IsManual,

        //            Segments =
        //                segments
        //        };
        //    }
    }
}
