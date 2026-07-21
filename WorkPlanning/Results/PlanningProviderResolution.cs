 

namespace HRM.WorkPlanning.Results
{
    public sealed class PlanningProviderResolution
    {
        public int PlanningProviderId { get; init; }

        public string Code { get; init; } = string.Empty;

        public bool UsesLegacyShift =>Code.Equals("LEGACY_SHIFT",StringComparison.OrdinalIgnoreCase);

        public bool UsesWorkPlanning =>Code.Equals("WORK_PLANNING",StringComparison.OrdinalIgnoreCase);
    }
}

