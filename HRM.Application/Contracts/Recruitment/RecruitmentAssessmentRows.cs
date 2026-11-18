namespace HRM.Application.Contracts.Recruitment;
public sealed record RecruitmentAssessmentRow(int Id,string Type,string TemplateCode,DateTime ScheduledAtUtc,string Status);
public sealed record RecruitmentScoringTemplateRow(string Code,string Name,string DefinitionJson);
public sealed record RecruitmentAssessmentWorkspace(IReadOnlyList<RecruitmentAssessmentRow> Assessments,IReadOnlyList<RecruitmentScoringTemplateRow> Templates);
