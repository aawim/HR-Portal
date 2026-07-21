using HRM.Enum;

namespace HRM.DTOs.Security
{
    public class AccessResultDto
    {
        /// <summary>
        /// Was access granted?
        /// </summary>
        public bool Allowed { get; set; }

        /// <summary>
        /// Machine-readable reason.
        /// Example:
        /// PermissionDenied
        /// NoActiveJob
        /// NotSupervisor
        /// AlreadyApproved
        /// </summary>
        public AccessCode Code { get; set; }

        /// <summary>
        /// Human-readable message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional details for debugging or logging.
        /// </summary>
        public List<string> Details { get; set; } = new();

        /// <summary>
        /// Additional data if required.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        

    }
}
