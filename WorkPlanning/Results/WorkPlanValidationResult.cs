namespace HRM.WorkPlanning.Results
{
    public class WorkPlanValidationResult
    {
        public bool IsValid => Errors.Count == 0;

        public List<string> Errors { get; } = new();

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
                AddError(error);
            }
        }
    }
}
