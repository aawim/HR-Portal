using HRM.Models;
namespace HRM.Services;

public sealed record ChatRequestDetail(string Label, string Value);

public sealed record ChatRequestDetails(string Reference, string Type, string Status, string Applicant, DateTime AppliedAtUtc,
    DateTime? EffectiveDate, List<ChatRequestDetail> Details);
