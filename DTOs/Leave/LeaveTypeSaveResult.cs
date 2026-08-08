namespace HRM.DTOs.Leave
{
    public class LeaveTypeSaveResult
    {
        public bool Success { get; set; }

        public bool IsSystemType { get; set; }
        public int? LeaveTypeId { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public static LeaveTypeSaveResult Successful(
            int leaveTypeId,
            string message)
        {
            return new LeaveTypeSaveResult
            {
                Success = true,
                LeaveTypeId = leaveTypeId,
                Message = message
            };
        }

        public static LeaveTypeSaveResult Failure(
            string message)
        {
            return new LeaveTypeSaveResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
