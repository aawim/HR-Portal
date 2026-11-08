namespace HRM.Application.Contracts.Pages;
public sealed record CredentialRequirementInput
{
    public long Id {get;set;}
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
public sealed record QualificationConditionInput
{
    public long Id {get;set;}
    public string Qualification {get;set;}="";
    public int? MinimumLevel {get;set;}
    public int? MaximumLevel {get;set;}
    public string FieldOfStudy {get;set;}="";
    public bool AccreditedRequired {get;set;}=true;
}
public sealed record QualificationPathwayInput
{
    public long Id {get;set;}
    public string Name {get;set;}="";
    public int RelevantExperienceMonths {get;set;}
    public int PostQualificationMonths {get;set;}
    public int ManagerialExperienceMonths {get;set;}
    public string Conditions {get;set;}="";
    public string SourceReference {get;set;}="";
    public List<QualificationConditionInput> Qualifications {get;set;}=[];
}
