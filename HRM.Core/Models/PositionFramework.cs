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
}
public sealed class FrameworkJobMatrix
{
    public int JobMatrixId {get;set;}
    public string MatrixCode {get;set;}="";
    public string Name {get;set;}="";
    public decimal BasicSalary {get;set;}
    public bool IsActive {get;set;}
}
public sealed class FrameworkJobTitle
{
    public int JobTitleId {get;set;}
    public string Name {get;set;}="";
    public bool IsActive {get;set;}
}
