namespace HRM.Resources
{
    public class PayrollResource : ResourceBase
    {
        public int PayrollID { get; set; }

        public int PayrollStateID { get; set; }

        public bool IsProcessed { get; set; }

        public bool IsApproved { get; set; }
    }
}
