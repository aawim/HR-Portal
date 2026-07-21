namespace HRM.DTOs
{
    public class ContactField
    {
        public int TypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Format { get; set; }
        public string? Value { get; set; }
    }
}
