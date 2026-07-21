using HRM.Resources;

namespace HRM.Services.Interfaces
{

    public interface ILeaveResourceBuilder : IResourceBuilder<LeaveResource, int>
    {
    }


    //public interface ILeaveResourceBuilder : IResourceBuilder<LeaveResource, int>
    //{
    //    Task<LeaveResource?> BuildAsync(LeaveResource leaveRequestId);
    //}
}
