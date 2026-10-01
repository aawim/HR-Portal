namespace HRM.Enum
{
    public enum AttendanceEvaluationIssueType
    {
        Unknown = 0,

        MissingCheckIn = 1,

        MissingCheckOut = 2,

        LateCheckIn = 3,

        EarlyCheckOut = 4,

        UnresolvedClock = 5,

        ClockOutsideWindow = 6,

        MissingBreakOut = 7,

        MissingBreakIn = 8,

        IncompleteAttendance = 9,

        NoWorkPlan = 10
    }
}
