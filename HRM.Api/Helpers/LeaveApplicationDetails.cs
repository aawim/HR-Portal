using HRM.DTOs.Leave;

namespace HRM.Helpers
{
    public static class LeaveApplicationDetails
    {
        public static string Build(LeaveApplicationDto model)
        {
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(model.Explanation)) details.Add(model.Explanation.Trim());
            Add("Certificate", model.MedicalCertificateNo);
            Add("Hospital", model.HospitalName);
            Add("Destination", model.Destination);
            Add("Purpose", model.Purpose);
            Add("Phone", model.Phone);
            Add("Email", model.ContactEmail);
            return string.Join(Environment.NewLine, details);

            void Add(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value)) details.Add($"{label}: {value.Trim()}");
            }
        }
    }
}
