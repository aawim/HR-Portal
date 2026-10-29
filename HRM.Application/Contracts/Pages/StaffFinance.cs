using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;

public sealed record FinanceOption(long Id, string Name);
public sealed record AllowanceTypeInput(long Id, string Name, bool IsActive = true);
public sealed record PositionAllowanceInput(int PositionId, long AllowanceTypeId, int CurrencyTypeId, decimal Amount,
    int FrequencyMonths, DateTime FromDate, DateTime? ToDate, string MultiplePositionPolicy = "Once");
public sealed record DeductionPolicyInput(int CurrencyTypeId, decimal MaximumPerMonth, decimal MaximumPercentOfBasic,
    decimal MinimumTakeHome, decimal MaximumOutstanding, int MaximumAgreements, DateTime FromDate);
public sealed record FinanceApplication
{
    public Guid ClientRequestId { get; init; } = Guid.NewGuid();
    public string Kind { get; init; } = "Allowance";
    public int JobId { get; init; }
    public int PayrollPeriodId { get; init; }
    public long AllowanceTypeId { get; init; }
    public long AgreementId { get; init; }
    public int BusinessEntityId { get; init; }
    public int CurrencyTypeId { get; init; }
    public decimal Amount { get; init; }
    public int InstalmentCount { get; init; } = 12;
    public int FrequencyMonths { get; init; } = 1;
    public DateTime FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string Category { get; init; } = "Loan";
    public string Reference { get; init; } = "";
    public string Reason { get; init; } = "";
}
public sealed record FinanceRequestRow(int Id, int StateId, string Reference, string Staff, string Kind, string Status,
    FinanceApplication Application, bool CanApprove, bool CanCancel);
public sealed record FinanceInstalmentRow(long Id, DateTime DueDate, decimal Amount, decimal Paid, int Version, bool Superseded);
public sealed record FinanceAgreementRow(long Id, int JobId, string Staff, string Reference, string Category, int CurrencyTypeId,
    decimal Total, decimal Outstanding, List<FinanceInstalmentRow> Instalments);
public sealed record FinanceAllowanceRow(long Id, int JobId, string Staff, string Type, int CurrencyTypeId, decimal Amount,
    DateTime FromDate, DateTime? ToDate, int FrequencyMonths, string Source);
public sealed record FinanceRuleRow(long Id, PositionAllowanceInput Rule, string Position, string Allowance);
public sealed record FinanceDocumentInfo(long Id, int RequestId, string Name, string ContentType);
public sealed record FinanceDocumentUpload(int RequestId, string Name, string ContentType, byte[] Content);
public sealed record FinanceDocumentDownload(string Name, string ContentType, byte[] Content);
public sealed record FinanceConfiguration(List<FinanceOption> AllowanceTypes, List<FinanceOption> Positions,
    List<FinanceOption> Currencies, List<FinanceRuleRow> Rules, List<DeductionPolicyInput> Policies, bool CanConfigure)
{
    public List<AllowanceTypeInput> TypeDefinitions { get; init; } = [];
    public List<FinanceOption> PayrollPeriods { get; init; } = [];
}
public sealed record FinanceWorkspace(List<FinanceRequestRow> Requests, List<FinanceAgreementRow> Agreements,
    List<FinanceAllowanceRow> Allowances, List<FinanceDocumentInfo> Documents, bool CanManage, bool CanPost);
public sealed record FinancePayrollLine(int JobId, int IndividualId, string Staff, string SourceKey, string Kind, string Description,
    int CurrencyTypeId, decimal Amount, long? InstalmentId = null, long? AllowanceId = null, long? PositionRuleId = null, int? JobPositionId = null);
public sealed record FinancePayrollPreview(DateTime Month, List<FinancePayrollLine> Lines, List<string> Issues, string Fingerprint = "");
public sealed record FinancePayrollInput(DateTime Month, int PayrollCycleRequestId, string Reference, Guid ClientBatchId, List<int> JobIds, string ExpectedFingerprint = "");
public sealed record FinancePayrollHistory(long Id, DateTime Month, string Reference, DateTime PostedAtUtc, List<FinancePayrollLine> Lines);
public interface IStaffFinance
{
    Task<FinanceConfiguration> GetConfigurationAsync();
    Task<ServiceResult> SaveAllowanceTypeAsync(AllowanceTypeInput input);
    Task<ServiceResult> AddPositionRuleAsync(PositionAllowanceInput input);
    Task<ServiceResult> EndPositionRuleAsync(long id, DateTime lastEntitlementDate);
    Task<ServiceResult> AddDeductionPolicyAsync(DeductionPolicyInput input);
    Task<FinanceWorkspace> LoadAsync(int? staffId = null);
    Task<List<FinanceInstalmentRow>> PreviewAgreementAsync(FinanceApplication input);
    Task<ServiceResult> SubmitAsync(FinanceApplication input);
    Task<ServiceResult> ActAsync(int requestId, int expectedState, string action, string? comment);
    Task<ServiceResult> UploadDocumentAsync(FinanceDocumentUpload input);
    Task<FinanceDocumentDownload> DownloadDocumentAsync(long id);
    Task<FinancePayrollPreview> PreviewPayrollAsync(DateTime month, List<int> jobIds);
    Task<List<FinanceOption>> GetPayrollCyclesAsync(DateTime month);
    Task<ServiceResult> PostPayrollAsync(FinancePayrollInput input);
    Task<List<FinancePayrollHistory>> PayrollHistoryAsync(int? staffId = null);
}
