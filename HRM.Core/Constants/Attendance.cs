namespace HRM.Constants
{
    public static class AttendanceResolutionStatusIds
    {
        public const int Pending = 1;
        public const int Resolved = 2;
        public const int NoAssignment = 3;
        public const int NoSegment = 4;
        public const int Duplicate = 5;
        public const int Invalid = 6;
        public const int Ignored = 7;
        public const int Manual = 8;
        public const int NoWorkPlan = 9;
        public const int OutsideResolutionWindow = 10;
        public const int DuplicatePunch = 11;
        public const int Conflict = 12;
        public const int PartiallyResolved = 13;
    }

    public static class AttendanceReconciliationPolicy
    {
        public const string CurrentVersion = "v2.0-OfficialPriority";
        public const int DefaultDuplicateSuppressionSeconds = 120;
    }
}
