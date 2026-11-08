namespace HRM.Models.Payroll;

public enum PayrollRunType
{
    RegularMonthly = 1,
    Supplementary = 2,
    OffCycle = 3,
    FinalSettlement = 4,
    ArrearsAdjustment = 5
}

public enum PayrollPeriodStatus
{
    Draft = 1,
    Open = 2,
    Locked = 3,
    Closed = 4,
    Archived = 5
}

public enum PayrollRunStatus
{
    Draft = 1,
    Preparing = 2,
    ReadyForCalculation = 3,
    Queued = 4,
    Calculating = 5,
    Calculated = 6,
    ValidationFailed = 7,
    ReadyForReview = 8,
    UnderReview = 9,
    Approved = 10,
    Finalised = 11,
    PaymentPrepared = 12,
    PaymentSubmitted = 13,
    PartiallyPaid = 14,
    Paid = 15,
    Closed = 16,
    Cancelled = 17,
    Failed = 18
}

public enum PayrollBatchStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Retrying = 5,
    Cancelled = 6
}

public enum SalaryComponentType
{
    BasicSalary = 1,
    FixedAllowance = 2,
    VariableAllowance = 3,
    DutyAllowance = 4,
    ShiftAllowance = 5,
    Overtime = 6,
    Bonus = 7,
    Reimbursement = 8,
    EmployeePension = 9,
    EmployeeTax = 10,
    UnpaidLeaveDeduction = 11,
    SalaryAdvanceDeduction = 12,
    LoanDeduction = 13,
    OtherDeduction = 14,
    EmployerPension = 15,
    EmployerStatutory = 16,
    AttendanceLatenessDeduction = 17,
    AttendanceAbsenceDeduction = 18
}

public enum CalculationMethodType
{
    FlatAmount = 1,
    PercentageOfBasic = 2,
    PercentageOfGross = 3,
    PositionGradeMatrix = 4,
    DailyRateProration = 5,
    HourlyRateOvertime = 6
}

public enum PayrollJobType
{
    PrepareSnapshot = 1,
    CalculateBatch = 2,
    ValidateRun = 3,
    FinaliseRun = 4,
    GeneratePayslips = 5,
    PreparePayments = 6
}

public enum PayrollJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    RetryScheduled = 5,
    Cancelled = 6
}

public enum PaymentBatchStatus
{
    Draft = 1,
    Prepared = 2,
    Exported = 3,
    Submitted = 4,
    PartiallyPaid = 5,
    Reconciled = 6,
    Failed = 7
}

public enum PayrollIssueSeverity
{
    Warning = 1,
    Blocking = 2
}
