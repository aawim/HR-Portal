using HRM.DTOs.Security;
using HRM.Resources;

namespace HRM.Services.Interfaces
{
    public interface IBusinessAccessService
    {
        #region Individual

        Task<AccessResultDto> CanViewIndividualAsync(int individualId);

        Task<AccessResultDto> CanViewIndividualAsync(IndividualResource resource);

        Task<AccessResultDto> CanEditIndividualAsync(int individualId);

        Task<AccessResultDto> CanEditIndividualAsync(IndividualResource resource);

        #endregion


        #region Job

        Task<AccessResultDto> CanViewJobAsync(int jobId);

        Task<AccessResultDto> CanEditJobAsync(int jobId);

        Task<AccessResultDto> CanTerminateJobAsync(int jobId);

        #endregion


        #region Leave
        Task<AccessResultDto> CanApplyLeaveAsync();
        Task<AccessResultDto> CanViewLeaveAsync(int leaveRequestId);
        Task<AccessResultDto> CanViewLeaveAsync(LeaveResource resource);

        Task<AccessResultDto> CanEditLeaveAsync(int requestId);

        Task<AccessResultDto> CanEditLeaveAsync(LeaveResource resource);

        Task<AccessResultDto> CanApproveLeaveAsync(int requestId);

        Task<AccessResultDto> CanApproveLeaveAsync(LeaveResource resource);

        Task<AccessResultDto> CanRejectLeaveAsync(int requestId);

        Task<AccessResultDto> CanRejectLeaveAsync(LeaveResource resource);

        Task<AccessResultDto> CanCancelLeaveAsync(int requestId);

        Task<AccessResultDto> CanCancelLeaveAsync(LeaveResource resource);

        //Task<AccessResultDto> CanDeleteLeaveAsync(int requestId);

        //Task<AccessResultDto> CanDeleteLeaveAsync(LeaveResource resource);


        #endregion


        #region Attendance

        //Task<AccessResultDto> CanViewAttendanceAsync(int attendanceId);

        //Task<AccessResultDto> CanEditAttendanceAsync(int attendanceId);


        Task<AccessResultDto> CanApproveAttendanceRequestAsync(int requestId);

        Task<AccessResultDto> CanApproveAttendanceRequestAsync(AttendanceRequestResource resource);


        Task<AccessResultDto> CanRejectAttendanceRequestAsync(int requestId);


        Task<AccessResultDto> CanRejectAttendanceRequestAsync(AttendanceRequestResource resource);



        #endregion


        #region Payroll

        Task<AccessResultDto> CanViewPayrollAsync(int payrollId);

        Task<AccessResultDto> CanProcessPayrollAsync(int payrollId);

        Task<AccessResultDto> CanApprovePayrollAsync(int payrollId);

        #endregion
    }
}
