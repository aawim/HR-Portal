using HRM.Models.WorkPlanning;
using System.Security.Cryptography.Xml;

namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkAssignmentOwnershipResult
    {
        public long? WorkAssignmentTransferId { get; set; }
        public bool Success { get; set; }

        public string Message { get; set; }
            = string.Empty;

        public long WorkAssignmentId { get; set; }

        public long WorkAssignmentOwnerId { get; set; }

        public long PreviousWorkAssignmentOwnerId { get; set; }  
        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public static WorkAssignmentOwnershipResult Failed(
            string message)
        {
            return new WorkAssignmentOwnershipResult
            {
                Success = false,
                Message = message
            };
        }



        //public bool Success { get; set; }

        //public string Message { get; set; }
        //    = string.Empty;

        //public long WorkAssignmentId { get; set; }

        //public long WorkAssignmentOwnerId { get; set; }

        //public long? PreviousWorkAssignmentOwnerId { get; set; }

        //public int IndividualId { get; set; }

        //public int JobId { get; set; }

        //public DateTime? EffectiveFrom { get; set; }

        //public DateTime? EffectiveTo { get; set; }

        public static WorkAssignmentOwnershipResult Failed(
            string message,
            long workAssignmentId = 0)
        {
            return new WorkAssignmentOwnershipResult
            {
                Success = false,
                Message = message,
                WorkAssignmentId = workAssignmentId
            };
        }

        //public static WorkAssignmentOwnershipResult SuccessMessage(
       
        //WorkAssignmentOwnershipResult transfer,
        //WorkAssignmentOwnershipResult newOwner,
        //WorkAssignmentOwnershipResult currentOwner,
        //WorkAssignment assignment,
        // string message,
        // long workAssignmentId = 0



        //    )
        //{
        //    return new WorkAssignmentOwnershipResult
        //    {
        //        Success = true,
        //        Message = $"Work assignment '{assignment.Name}' was transferred successfully.",

        //        WorkAssignmentId = assignment.WorkAssignmentId,
        //        WorkAssignmentTransferId =
        //         transfer.WorkAssignmentTransferId,

        //                PreviousWorkAssignmentOwnerId =
        //         currentOwner.WorkAssignmentOwnerId,

        //                WorkAssignmentOwnerId =
        //         newOwner.WorkAssignmentOwnerId,

        //                IndividualId =
        //         newOwner.IndividualId,

        //                JobId =
        //         newOwner.JobId,

        //                EffectiveFrom =
        //         newOwner.EffectiveFrom
        //            };
        //}



    }
}
