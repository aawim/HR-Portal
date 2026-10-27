using HRM.DTOs.Leave;

namespace HRM.Services.Interfaces.Leave;

public interface ILeaveReceiptService
{
    Task<LeaveReceiptDto?> GetMyReceiptAsync(int leaveId);
}
