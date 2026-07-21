namespace HRM.Resources
{
    public abstract class ResourceBase
    {
        /// <summary>
        /// Owner of the resource.
        /// </summary>
        public int IndividualID { get; set; }

        /// <summary>
        /// Organisation owning the resource.
        /// </summary>
        public int OrganisationID { get; set; }

        /// <summary>
        /// Is the resource active?
        /// </summary>
        public bool IsActive { get; set; }
    }
}
