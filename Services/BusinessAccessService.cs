using HRM.Authorization;
using HRM.DTOs.Security;
using HRM.Enum;
using HRM.Resources;
using HRM.Services.Interfaces;


namespace HRM.Services
{


    public class BusinessAccessService : IBusinessAccessService
    {
        private readonly IUserAccessService _access;

        private readonly IIndividualResourceBuilder _individualBuilder;

        private readonly ILeaveResourceBuilder _leaveBuilder;

        private readonly IAttendanceRequestResourceBuilder _attendanceRequestBuilder;

        // Later
        // private readonly IJobResourceBuilder _jobBuilder;
        // private readonly IAttendanceResourceBuilder _attendanceBuilder;
        // private readonly IPayrollResourceBuilder _payrollBuilder;

        public BusinessAccessService(
            IUserAccessService access,
            IIndividualResourceBuilder individualBuilder,
            ILeaveResourceBuilder leaveBuilder,
            IAttendanceRequestResourceBuilder attendanceRequestBuilder  )
        {
            _access = access;
            _individualBuilder = individualBuilder;
            _leaveBuilder = leaveBuilder;
            _attendanceRequestBuilder = attendanceRequestBuilder;
        }

        #region Individual

        public async Task<AccessResultDto> CanViewIndividualAsync(int individualId)
        {
            var resource = await _individualBuilder.BuildAsync(individualId);

            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.UserNotFound,
                    "Individual not found.");
            }

            return await CanViewIndividualAsync(resource);
        }

        public async Task<AccessResultDto> CanViewIndividualAsync(
            IndividualResource resource)
        {
            var result = await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;

            result = await RequirePermissionAsync(
                Permissions.IndividualView);

            if (!result.Allowed)
                return result;

            result = await RequireOrganisationAsync(
                resource.OrganisationID);

            if (!result.Allowed)
                return result;

            return AccessResult.Allow();
        }

        public async Task<AccessResultDto> CanEditIndividualAsync(
            int individualId)
        {
            var resource =
                await _individualBuilder.BuildAsync(individualId);


            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Individual record not found.");
            }


            return await CanEditIndividualAsync(resource);
        }

        public async Task<AccessResultDto> CanEditIndividualAsync(IndividualResource resource)
        {
            // 1. User eligibility
            var result =
                await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;



            // 2. Permission
            result =
                await RequirePermissionAsync(
                    Permissions.IndividualEdit);

            if (!result.Allowed)
                return result;



            // 3. Organisation scope
            result =
                await RequireOrganisationAsync(
                    resource.OrganisationID);

            if (!result.Allowed)
                return result;



            // 4. Business rule
            var context =
                await _access.GetContextAsync();


            var canEdit =
                context.IsSuperAdministrator ||
                context.IsHRStaff ||
                context.IsHeadOfOrganisation ||
                resource.IsOwnRecord;


            if (!canEdit)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You do not have permission to edit this individual.");
            }

 



            return AccessResult.Allow();
        }

        #endregion


        #region Leave



        public async Task<AccessResultDto> CanApplyLeaveAsync()
        {
            // 1. User must be valid
            var result =
                await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;


            // 2. Permission check
            result =
                await RequirePermissionAsync(
                    Permissions.LeaveCreate);

            if (!result.Allowed)
                return result;


            // 3. Must have active employment
            var context =
                await _access.RequireContextAsync();


            if (context.ActiveJob == null)
            {
                return AccessResult.Deny(
                    AccessCode.NoActiveJob,
                    "You do not have an active job.");
            }


            // 4. Everything is OK
            return AccessResult.Allow();
        }

        public async Task<AccessResultDto> CanViewLeaveAsync(LeaveResource resource)
        {
            var result = await RequireLeaveAccessAsync(resource, Permissions.LeaveView);

            if (!result.Allowed)
                return result;


            result = RequirePendingLeave(resource);

            if (!result.Allowed)
                return result;


            var context =
                await _access.RequireContextAsync();


            var canApprove =
                context.IsSuperAdministrator ||
                context.IsHeadOfOrganisation ||
                context.IsSupervisor ||
                context.IsHRHead;


            if (!canApprove)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You are not allowed to approve this leave.");
            }


            return AccessResult.Allow();
        }

        public async Task<AccessResultDto> CanViewLeaveAsync(int requestId)
        {
            var resource = await _leaveBuilder.BuildAsync(requestId);

            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Leave request not found.");
            }

            return await CanViewLeaveAsync(resource);
        }

        public async Task<AccessResultDto> CanApproveLeaveAsync(LeaveResource resource)
        {
            var result = await RequireLeaveAccessAsync(resource,Permissions.LeaveApprove);

            if (!result.Allowed)
                return result;


            result = RequirePendingLeave(resource);

            if (!result.Allowed)
                return result;


            var context =
                await _access.RequireContextAsync();


            var canApprove =
                context.IsSuperAdministrator ||
                context.IsHeadOfOrganisation ||
                context.IsSupervisor ||
                context.IsHRHead;


            if (!canApprove)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You are not allowed to approve this leave.");
            }


            return AccessResult.Allow();
        }

        public async Task<AccessResultDto> CanApproveLeaveAsync(int requestId)
        {
            var resource =await _leaveBuilder.BuildAsync(requestId);

            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Leave request not found.");
            }

            return await CanApproveLeaveAsync(resource);
        }


        public async Task<AccessResultDto> CanEditLeaveAsync(LeaveResource resource)
        {
            // 1. User eligibility
            var result =
                await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;



            // 2. Permission
            result =
                await RequirePermissionAsync(
                    Permissions.LeaveEdit);

            if (!result.Allowed)
                return result;



            // 3. Organisation access
            result =
                await RequireOrganisationAsync(
                    resource.OrganisationID);

            if (!result.Allowed)
                return result;



            // 4. State validation

            if (resource.IsApproved)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Approved leave cannot be edited.");
            }


            if (resource.IsRejected)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Rejected leave cannot be edited.");
            }


            if (resource.IsCancelled)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Cancelled leave cannot be edited.");
            }



            // 5. Authority

            var context =
                await _access.RequireContextAsync();


            var canEdit =
                resource.IndividualID ==
                    context.IndividualId

                ||

                context.IsSuperAdministrator

                ||

                context.IsHRStaff

                ||

                context.IsHRHead

                ||

                context.IsHeadOfOrganisation

                ||

                context.IsSupervisor;



            if (!canEdit)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You are not allowed to edit this leave.");
            }


            return AccessResult.Allow();
        }


        public async Task<AccessResultDto> CanEditLeaveAsync(int requestId)
        {
            var resource =
                await _leaveBuilder.BuildAsync(requestId);


            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Leave request not found.");
            }


            return await CanEditLeaveAsync(resource);
        }


        public async Task<AccessResultDto> CanRejectLeaveAsync(LeaveResource resource)
        {
            var result = await RequireLeaveAccessAsync(resource, Permissions.LeaveReject);

            if (!result.Allowed)
                return result;


            result = RequirePendingLeave(resource);

            if (!result.Allowed)
                return result;


            var context =
                await _access.RequireContextAsync();


            var canApprove =
                context.IsSuperAdministrator ||
                context.IsHeadOfOrganisation ||
                context.IsSupervisor ||
                context.IsHRHead;


            if (!canApprove)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You are not allowed to approve this leave.");
            }


            return AccessResult.Allow();
        }

        public async Task<AccessResultDto> CanRejectLeaveAsync(int requestId)
        {
            var resource = await _leaveBuilder.BuildAsync(requestId);

            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Leave request not found.");
            }

            return await CanRejectLeaveAsync(resource);
        }


        public async Task<AccessResultDto> CanCancelLeaveAsync(LeaveResource resource)
        {
            var result = await RequireLeaveAccessAsync(resource, Permissions.LeaveCancel);

            if (!result.Allowed)
                return result;


            result = RequirePendingLeave(resource);

            if (!result.Allowed)
                return result;


            var context =
                await _access.RequireContextAsync();


            var canApprove =
                context.IsSuperAdministrator ||
                context.IsHeadOfOrganisation ||
                context.IsSupervisor ||
                context.IsHRHead;


            if (!canApprove)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You are not allowed to approve this leave.");
            }


            return AccessResult.Allow();
        }

        public async Task<AccessResultDto> CanCancelLeaveAsync(int requestId)
        {
            var resource = await _leaveBuilder.BuildAsync(requestId);

            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Leave request not found.");
            }

            return await CanCancelLeaveAsync(resource);
        }




         

        #endregion


        #region Job

        public Task<AccessResultDto> CanViewJobAsync(int jobId)
        {
            throw new NotImplementedException();
        }

        public Task<AccessResultDto> CanEditJobAsync(int jobId)
        {
            throw new NotImplementedException();
        }

        public Task<AccessResultDto> CanTerminateJobAsync(int jobId)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region Attendance

        //public Task<AccessResultDto> CanViewAttendanceAsync(int attendanceId)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<AccessResultDto> CanEditAttendanceAsync(int attendanceId)
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<AccessResultDto> CanApproveAttendanceRequestAsync(AttendanceRequestResource resource)
        {
            // 1. User eligibility
            var result =
                await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;



            // 2. Permission
            result =
                await RequirePermissionAsync(Permissions.AttendanceApprove);

            if (!result.Allowed)
                return result;



            // 3. Organisation access
            result =
                await RequireOrganisationAsync(
                    resource.OrganisationID);

            if (!result.Allowed)
                return result;



            // 4. Request state validation

            if (!resource.IsPending)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Only pending attendance requests can be approved.");
            }



            // 5. Attendance state validation

            if (!resource.IsAttendanceInvalid)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Only invalid attendance logs can be approved.");
            }



            // 6. Approval authority

            var context =
                await _access.RequireContextAsync();


            var canApprove =
                context.IsSuperAdministrator

                ||

                context.IsHRHead

                ||

                context.IsHeadOfOrganisation

                ||

                context.IsSupervisor;



            if (!canApprove)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You are not allowed to approve attendance requests.");
            }



            return AccessResult.Allow();
        }

        public async Task<AccessResultDto> CanApproveAttendanceRequestAsync(int requestId)
        {
            var resource =
                await _attendanceRequestBuilder.BuildAsync(requestId);


            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Attendance request not found.");
            }


            return await CanApproveAttendanceRequestAsync(resource);
        }


        public async Task<AccessResultDto> CanRejectAttendanceRequestAsync(AttendanceRequestResource resource)
        {
            // 1. User eligibility
            var result =
                await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;



            // 2. Permission
            result =
                await RequirePermissionAsync(
                    Permissions.AttendanceReject);

            if (!result.Allowed)
                return result;



            // 3. Organisation access
            result =
                await RequireOrganisationAsync(
                    resource.OrganisationID);

            if (!result.Allowed)
                return result;



            // 4. Request state validation

            if (!resource.IsPending)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Only pending attendance requests can be rejected.");
            }



            // 5. Attendance validation

            if (!resource.IsAttendanceInvalid)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Only invalid attendance logs can be rejected.");
            }



            // 6. Authority check

            var context =
                await _access.RequireContextAsync();


            var canReject =
                context.IsSuperAdministrator

                ||

                context.IsHRHead

                ||

                context.IsHeadOfOrganisation

                ||

                context.IsSupervisor;



            if (!canReject)
            {
                return AccessResult.Deny(
                    AccessCode.BusinessRuleDenied,
                    "You are not allowed to reject attendance requests.");
            }



            return AccessResult.Allow();
        }
        public async Task<AccessResultDto> CanRejectAttendanceRequestAsync(int requestId)
        {
            var resource =
                await _attendanceRequestBuilder.BuildAsync(requestId);


            if (resource == null)
            {
                return AccessResult.Deny(
                    AccessCode.ResourceNotFound,
                    "Attendance request not found.");
            }


            return await CanRejectAttendanceRequestAsync(resource);
        }

        #endregion


        #region Payroll

        public Task<AccessResultDto> CanViewPayrollAsync(int payrollId)
        {
            throw new NotImplementedException();
        }

        public Task<AccessResultDto> CanProcessPayrollAsync(int payrollId)
        {
            throw new NotImplementedException();
        }

        public Task<AccessResultDto> CanApprovePayrollAsync(int payrollId)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region Helpers

        private async Task<AccessResultDto> RequireLeaveAccessAsync(LeaveResource resource,string permission)
        {
            // 1. User eligibility
            var result = await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;

            // 2. Permission
            result = await RequirePermissionAsync(permission);

            if (!result.Allowed)
                return result;

            // 3. Organisation scope
            result = await RequireOrganisationAsync(resource.OrganisationID);

            if (!result.Allowed)
                return result;

            return AccessResult.Allow();
        }


        private async Task<AccessResultDto> RequireAttendanceAccessAsync(AttendanceRequestResource resource, string permission)
        {
            // 1. User eligibility
            var result = await RequireEligibleUserAsync();

            if (!result.Allowed)
                return result;

            // 2. Permission
            result = await RequirePermissionAsync(permission);

            if (!result.Allowed)
                return result;

            // 3. Organisation scope
            result = await RequireOrganisationAsync(resource.OrganisationID);

            if (!result.Allowed)
                return result;

            return AccessResult.Allow();
        }




        private AccessResultDto RequirePendingLeave(LeaveResource resource)
        {
            if (resource.IsCancelled)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Leave has already been cancelled.");
            }

            if (resource.IsRejected)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Leave has already been rejected.");
            }

            if (resource.IsApproved)
            {
                return AccessResult.Deny(
                    AccessCode.InvalidState,
                    "Leave has already been approved.");
            }

            return AccessResult.Allow();
        }
        private async Task<AccessResultDto> RequireEligibleUserAsync()
        {
            if (await _access.IsEligibleAsync())
                return AccessResult.Allow();

            return AccessResult.Deny(
                AccessCode.UserNotEligible,
                "The current user is not eligible.");
        }

        private async Task<AccessResultDto> RequirePermissionAsync(
            string permission)
        {
            if (await _access.HasPermissionAsync(permission))
                return AccessResult.Allow();

            return AccessResult.Deny(
                AccessCode.PermissionDenied,
                $"Missing permission '{permission}'.");
        }

        private async Task<AccessResultDto> RequireOrganisationAsync(
            int organisationId)
        {
            if (await _access.HasOrganisationAccessAsync(organisationId))
                return AccessResult.Allow();

            return AccessResult.Deny(
                AccessCode.OrganisationAccessDenied,
                "You do not have access to this organisation.");
        }

        #endregion
    }
}
