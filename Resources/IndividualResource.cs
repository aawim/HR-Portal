namespace HRM.Resources
{
    public class IndividualResource : ResourceBase
    {
       

        /// <summary>
        /// Current active Job.
        /// Null if no active employment.
        /// </summary>
        public int? JobID { get; set; }

         /// <summary>
        /// Current Organisation Structure (Department).
        /// </summary>
        public int? OrganisationStructureID { get; set; }

        /// <summary>
        /// Is the Individual currently employed?
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Is the current logged-in user viewing their own record?
        /// </summary>
        public bool IsOwnRecord { get; set; }

        /// <summary>
        /// Direct supervisor of this Individual.
        /// </summary>
        public int? SupervisorIndividualID { get; set; }

        /// <summary>
        /// Is the current user the supervisor of this Individual?
        /// </summary>
        public bool IsSupervisor { get; set; }

        /// <summary>
        /// Does the current user have HR Staff privileges?
        /// </summary>
        public bool IsHRStaff { get; set; }

        /// <summary>
        /// Is the current user the Head of Organisation?
        /// </summary>
        public bool IsHeadOfOrganisation { get; set; }

        public int? CurrentUserIndividualID { get; set; }
    }
}
 