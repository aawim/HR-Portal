using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services
{
    public class AttendanceLogDataLoader : IAttendanceLogDataLoader
    {
     

        private readonly IDbContextFactory<HrmTeContext> _dbfactory;

        public AttendanceLogDataLoader(IDbContextFactory<HrmTeContext> dbfactory)
        {
            _dbfactory = dbfactory;
        }

        public async Task<AttendanceLogDto?>GetEarliestCheckInAsync(int individualId,DateTime date)
        {
            using var db = await _dbfactory.CreateDbContextAsync();
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            // Use resolved clock type from AttendanceLogResolutions, excluding replaced duplicate punches
            return await db.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualId == individualId &&
                    x.AttendanceLogStateId == 1 &&
                    x.Date >= dayStart &&
                    x.Date < dayEnd &&
                    (x.AttendanceLogResolutions.Any(r =>
                        r.IsValid &&
                        r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved &&
                        r.AttendanceClockTypeId == (int)HRM.Enum.AttendanceClockType.CheckIn) ||
                     (!x.AttendanceLogResolutions.Any(r => r.IsValid) && x.InOutModeId == 1)))
                .OrderBy(x => x.Date)
                .Select(x => new AttendanceLogDto
                {
                    AttendanceLogID = x.AttendanceLogId,
                    AttendanceDeviceID = x.AttendanceDeviceId,
                    IndividualID = x.IndividualId,
                    OrganisationID = x.OrganisationId,
                    OrganisationStructureID = x.OrganisationStructureId,
                    InOutModeID = x.InOutModeId,
                    Year = x.Year,
                    Month = x.Month,
                    Day = x.Day,
                    Hour = x.Hour,
                    Minute = x.Minute,
                    Second = x.Second,
                    Date = x.Date,
                    AttendanceLogModeID = x.AttendanceLogModeId,
                    AttendanceLogStateID = x.AttendanceLogStateId,
                    OperationLogID = x.OperationLogId,
                    RelatedAttendanceLogID = x.RelatedAttendanceLogId,
                    ActualInOutMode = x.ActualInOutMode,
                    ResolvedClockType = HRM.Enum.AttendanceClockType.CheckIn
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AttendanceLogDto?>GetLatestCheckOutAsync(int individualId, DateTime date)
        {
            using var db = await _dbfactory.CreateDbContextAsync();
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            // Use resolved clock type from AttendanceLogResolutions, excluding replaced duplicate punches
            return await db.AttendanceLogs
              .AsNoTracking()
              .Where(x =>
                  x.IndividualId == individualId &&
                    x.AttendanceLogStateId == 1 &&
                    x.Date >= dayStart &&
                    x.Date < dayEnd &&
                    (x.AttendanceLogResolutions.Any(r =>
                        r.IsValid &&
                        r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved &&
                        r.AttendanceClockTypeId == (int)HRM.Enum.AttendanceClockType.CheckOut) ||
                     (!x.AttendanceLogResolutions.Any(r => r.IsValid) && x.InOutModeId == 2)))
              .OrderByDescending(x => x.Date)
              .Select(x => new AttendanceLogDto
              {
                  AttendanceLogID = x.AttendanceLogId,
                  AttendanceDeviceID = x.AttendanceDeviceId,
                  IndividualID = x.IndividualId,
                  OrganisationID = x.OrganisationId,
                  OrganisationStructureID = x.OrganisationStructureId,
                  InOutModeID = x.InOutModeId,
                  Year = x.Year,
                  Month = x.Month,
                  Day = x.Day,
                  Hour = x.Hour,
                  Minute = x.Minute,
                  Second = x.Second,
                  Date = x.Date,
                  AttendanceLogModeID = x.AttendanceLogModeId,
                  AttendanceLogStateID = x.AttendanceLogStateId,
                  OperationLogID = x.OperationLogId,
                  RelatedAttendanceLogID = x.RelatedAttendanceLogId,
                  ActualInOutMode = x.ActualInOutMode,
                  ResolvedClockType = HRM.Enum.AttendanceClockType.CheckOut
              })
              .FirstOrDefaultAsync();
        }

        public async Task<AttendanceLogDto?> GetLogsAsync(int individualId, DateTime from, DateTime to)
        {
            using var db = await _dbfactory.CreateDbContextAsync();
            return await db.AttendanceLogs
              .AsNoTracking()
              .Where(x =>
                  x.IndividualId == individualId &&
                    x.AttendanceLogStateId == 1 &&
                    x.Date >= from &&
                    x.Date <= to)
              .OrderBy(x => x.Date)
              .Select(x => new AttendanceLogDto
              {
                  AttendanceLogID = x.AttendanceLogId,
                  AttendanceDeviceID = x.AttendanceDeviceId,
                  IndividualID = x.IndividualId,
                  OrganisationID = x.OrganisationId,
                  OrganisationStructureID = x.OrganisationStructureId,
                  InOutModeID = x.InOutModeId,
                  Year = x.Year,
                  Month = x.Month,
                  Day = x.Day,
                  Hour = x.Hour,
                  Minute = x.Minute,
                  Second = x.Second,
                  Date = x.Date,
                  AttendanceLogModeID = x.AttendanceLogModeId,
                  AttendanceLogStateID = x.AttendanceLogStateId,
                  OperationLogID = x.OperationLogId,
                  RelatedAttendanceLogID = x.RelatedAttendanceLogId,
                  ActualInOutMode = x.ActualInOutMode
              })
              .FirstOrDefaultAsync();
        }

 


    }
}
