namespace HRM.Models;
public sealed class MatrixCredentialRequirement
{
    public long Id {get;set;}
    public int JobMatrixId {get;set;}
    public bool IsActive {get;set;}=true;
    public string Name {get;set;}="";
    public string Kind {get;set;}="Licence";
    public string Issuer {get;set;}="";
    public string Requirement {get;set;}="Mandatory";
    public string Condition {get;set;}="";
    public string Equivalents {get;set;}="";
    public int? ValidityMonths {get;set;}
    public bool RenewalRequired {get;set;}
    public decimal? TrainingHours {get;set;}
    public bool EvidenceRequired {get;set;}=true;
    public string VerificationInstructions {get;set;}="";
    public string SourceReference {get;set;}="";
}
public sealed class MatrixQualificationPathway
{
    public long Id {get;set;}
    public int JobMatrixId {get;set;}
    public bool IsActive {get;set;}=true;
    public string Name {get;set;}="";
    public int RelevantExperienceMonths {get;set;}
    public int PostQualificationMonths {get;set;}
    public int ManagerialExperienceMonths {get;set;}
    public string Conditions {get;set;}="";
    public string SourceReference {get;set;}="";
}
public sealed class MatrixQualificationCondition
{
    public long Id {get;set;}
    public long PathwayId {get;set;}
    public bool IsActive {get;set;}=true;
    public string Qualification {get;set;}="";
    public int? MinimumLevel {get;set;}
    public int? MaximumLevel {get;set;}
    public string FieldOfStudy {get;set;}="";
    public bool AccreditedRequired {get;set;}=true;
}
