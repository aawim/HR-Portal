namespace HRM.Enum
{
    public enum AttendanceObligationState
    {
        NotStarted = 0,
        ClockedIn = 1,
        OnBreak = 2,
        ClockedOut = 3,
        MissingClockIn = 4,
        MissingClockOut = 5,
        PartiallyResolved = 6,
        Conflict = 7,
        Completed = 8
    }
}
