namespace HRM.Models;
public sealed class PositionFramework
{
    public int FrameworkId {get;set;}
    public string Code {get;set;}="";
    public string Name {get;set;}="";
    public bool IsActive {get;set;}
}
public sealed class PositionMatrix
{
    public int PositionMatrixId {get;set;}
    public int OrganisationId {get;set;}
    public int? OrganisationStructureId {get;set;}
    public int JobMatrixId {get;set;}
    public int JobTitleId {get;set;}
    public int JobMatrixStepId {get;set;}
    public int ApprovedSlots {get;set;}
    public DateTime EffectiveFrom {get;set;}
    public DateTime? EffectiveTo {get;set;}
    public bool IsActive {get;set;}
    public int? OperationLogId {get;set;}
}
public sealed class FrameworkJobMatrix
{
    public int JobMatrixId {get;set;}
    public string MatrixCode {get;set;}="";
    public string Name {get;set;}="";
    public decimal BasicSalary {get;set;}
    public bool IsActive {get;set;}
    public int? FrameworkId {get;set;}
    public int? CurrencyTypeId {get;set;}
    public DateTime EffectiveFrom {get;set;}
    public DateTime? EffectiveTo {get;set;}
    public int? OperationLogId {get;set;}
}
public sealed class FrameworkJobTitle
{
    public int JobTitleId {get;set;}
    public string Name {get;set;}="";
    public bool IsActive {get;set;}
}
public sealed class FrameworkJobMatrixStep
{
    public int JobMatrixStepId {get;set;}
    public int JobMatrixId {get;set;}
    public int StepNumber {get;set;}
    public decimal JobAllowance {get;set;}
    public decimal? BasicSalaryOverride {get;set;}
    public bool IsActive {get;set;}
}
public sealed class FrameworkJobMatrixTitle
{
    public int JobMatrixId {get;set;}
    public int JobTitleId {get;set;}
}
public sealed class FrameworkJobMatrixBenefit
{
    public int JobMatrixBenefitId {get;set;}
    public int JobMatrixId {get;set;}
    public string BenefitName {get;set;}="";
    public decimal? Amount {get;set;}
    public string? FrequencyCode {get;set;}
}
public sealed class FrameworkPositionMatrixHistory
{
    public long PositionMatrixHistoryId {get;set;}
    public int PositionMatrixId {get;set;}
    public int? PreviousApprovedSlots {get;set;}
    public int NewApprovedSlots {get;set;}
    public string? ChangeReason {get;set;}
    public DateTime ChangedAt {get;set;}
    public int? OperationLogId {get;set;}
}
public sealed class FrameworkJobStepHistory
{
    public long JobStepHistoryId {get;set;}
    public int JobId {get;set;}
    public int PositionMatrixId {get;set;}
    public int JobMatrixStepId {get;set;}
    public DateTime EffectiveFrom {get;set;}
    public DateTime? EffectiveTo {get;set;}
    public decimal BasicSalarySnapshot {get;set;}
    public decimal JobAllowanceSnapshot {get;set;}
    public int? OperationLogId {get;set;}
}
