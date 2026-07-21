namespace HRM.DTOs
{
    public class CreateRequestDto
    {
        public int RequestTypeId { get; set; }

        public int OperationLogId { get; set; }

        public int? ApplicantBusinessEntityId { get; set; }

        public int? OrganisationBusinessEntityId { get; set; }

        public int? ServiceId { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public int? CurrentApplicationTab { get; set; }
    }
}
