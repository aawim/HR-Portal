using HRM.Models;
using HRM.Resources;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class AttendanceRequestResourceBuilder : IAttendanceRequestResourceBuilder
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;


        public AttendanceRequestResourceBuilder(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }



        public async Task<AttendanceRequestResource?> BuildAsync(
            int requestId)
        {
            using var db =
                await _dbFactory.CreateDbContextAsync();


            var data =
                await db.AttendanceLogRequests
                .AsNoTracking()
                .Where(x =>
                    x.RequestId == requestId)
                .Select(x => new
                {
                    x.AttendanceLogRequestId,

                    x.RequestId,

                    x.AttendanceLogId,

                    x.Comments,


                    RequestStateID =
                        x.Request.RequestStateId,


                    Attendance =
                        new
                        {
                            x.AttendanceLog.IndividualId,

                            x.AttendanceLog.OrganisationId,

                            x.AttendanceLog.OrganisationStructureId,

                            x.AttendanceLog.AttendanceLogStateId,

                            x.AttendanceLog.AttendanceLogModeId
                        }
                })
                .FirstOrDefaultAsync();



            if (data == null)
                return null;



            return new AttendanceRequestResource
            {
                AttendanceLogRequestID =
                    data.AttendanceLogRequestId,


                RequestID =
                    data.RequestId,


                AttendanceLogID =
                    data.AttendanceLogId,


                IndividualID =
                    data.Attendance.IndividualId,


                OrganisationID =
                    data.Attendance.OrganisationId,


                OrganisationStructureID =
                    data.Attendance.OrganisationStructureId,


                RequestStateID =
                    data.RequestStateID,


                AttendanceLogStateID =
                    data.Attendance.AttendanceLogStateId,


                AttendanceLogModeID =
                    data.Attendance.AttendanceLogModeId,


                Comments =
                    data.Comments
            };
        }
    }
}
