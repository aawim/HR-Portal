
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Attendance.Repositories
{
    public sealed class AttendanceLogResolutionRepository : IAttendanceLogResolutionRepository
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public AttendanceLogResolutionRepository(IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        public async Task<AttendanceLogResolution?>
        GetByAttendanceLogIdAsync(
            int attendanceLogId,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.AttendanceLogResolutions
                .AsNoTracking()
                .Include(x => x.AttendanceResolutionStatus)
                .Include(x => x.WorkPlan)
                .Include(x => x.WorkAssignment)
                .Include(x => x.WorkAssignmentSegment)
                .SingleOrDefaultAsync(
                    x =>
                        x.AttendanceLogId == attendanceLogId &&
                        x.IsValid,
                    cancellationToken);
        }

        public async Task<bool> HasActiveResolutionAsync(
            int attendanceLogId,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.AttendanceLogResolutions
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.AttendanceLogId == attendanceLogId &&
                        x.IsValid,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<AttendanceLogResolution>>
            GetByAssignmentAsync(
                long workAssignmentId,
                CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.AttendanceLogResolutions
                .AsNoTracking()
                .Include(x => x.AttendanceLog)
                .Include(x => x.AttendanceResolutionStatus)
                .Include(x => x.WorkAssignmentSegment)
                .Where(x =>
                    x.WorkAssignmentId == workAssignmentId &&
                    x.IsValid)
                .OrderBy(x => x.AttendanceLog.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<AttendanceLogResolution> AddAsync(
            AttendanceLogResolution resolution,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resolution);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var alreadyExists =
                await db.AttendanceLogResolutions
                    .AnyAsync(
                        x =>
                            x.AttendanceLogId ==
                                resolution.AttendanceLogId &&
                            x.IsValid,
                        cancellationToken);

            if (alreadyExists)
            {
                throw new InvalidOperationException(
                    $"Attendance log " +
                    $"{resolution.AttendanceLogId} already has " +
                    "an active resolution.");
            }

            resolution.CreatedDate =
                resolution.CreatedDate == default
                    ? DateTime.Now
                    : resolution.CreatedDate;

            resolution.ResolutionDate =
                resolution.ResolutionDate == default
                    ? DateTime.Now
                    : resolution.ResolutionDate;

            resolution.IsValid = true;

            db.AttendanceLogResolutions.Add(resolution);

            await db.SaveChangesAsync(cancellationToken);

            return resolution;
        }

        public async Task InvalidateAsync(
            long attendanceLogResolutionId,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var resolution =
                await db.AttendanceLogResolutions
                    .SingleOrDefaultAsync(
                        x =>
                            x.AttendanceLogResolutionId ==
                                attendanceLogResolutionId &&
                            x.IsValid,
                        cancellationToken);

            if (resolution is null)
            {
                throw new InvalidOperationException(
                    $"Active attendance resolution " +
                    $"{attendanceLogResolutionId} was not found.");
            }

            resolution.IsValid = false;

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
