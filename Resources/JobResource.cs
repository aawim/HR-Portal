namespace HRM.Resources
{
    public class JobResource : ResourceBase
    {
        public int JobID { get; set; }

        public int JobStateID { get; set; }

        public bool IsActive { get; set; }

        public int? SupervisorIndividualID { get; set; }
    }
}
